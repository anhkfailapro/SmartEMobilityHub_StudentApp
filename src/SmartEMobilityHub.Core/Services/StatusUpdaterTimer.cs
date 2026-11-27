using SmartEMobilityHub.Core.Adapters;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// Bộ định thời trung tâm (Sub3 Bảng 28/29). Mỗi 30 giây chạy tuần tự 6 bước;
/// mỗi bước có try/catch riêng nên lỗi một bước không chặn các bước còn lại (NFR 2.2.2).
/// </summary>
public sealed class StatusUpdaterTimer
{
    public const int ScanIntervalMs = ServiceConstants.ScanIntervalMs;
    public const int StaleThresholdMin = ServiceConstants.StaleThresholdMinutes;
    public const int ReservationHoldMin = ServiceConstants.ReservationHoldMinutes;

    private readonly NetworkStore _store;
    private readonly IClock _clock;
    private readonly HubStatusSyncService _hubSync;
    private readonly ThresholdMonitor _thresholdMonitor;
    private readonly AlertRetryQueue _retryQueue;
    private readonly AutoCancelService _autoCancel;
    private readonly ISmartSchedulingService _smartScheduling;
    private readonly ISystemLogger _logger;
    private readonly IScheduler _ticker;

    private IDisposable? _subscription;

    /// <summary>Hub đã được báo STALE_DATA trong đợt dữ liệu cũ hiện tại.</summary>
    private readonly HashSet<string> _staleAlertedHubs = new();

    public StatusUpdaterTimer(
        NetworkStore store,
        IClock clock,
        HubStatusSyncService hubSync,
        ThresholdMonitor thresholdMonitor,
        AlertRetryQueue retryQueue,
        AutoCancelService autoCancel,
        ISmartSchedulingService smartScheduling,
        ISystemLogger logger,
        IScheduler ticker)
    {
        _store = store;
        _clock = clock;
        _hubSync = hubSync;
        _thresholdMonitor = thresholdMonitor;
        _retryQueue = retryQueue;
        _autoCancel = autoCancel;
        _smartScheduling = smartScheduling;
        _logger = logger;
        // Bắt buộc truyền scheduler (DC-47): mặc định cũ là ThreadPoolScheduler chạy ở luồng nền,
        // trong khi NetworkStore và các ObservableCollection của UI không an toàn đa luồng.
        _ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
    }

    public bool IsRunning { get; private set; }

    public DateTime? LastTickAt { get; private set; }

    /// <summary>Bảng 29 — idempotent: gọi khi đang chạy thì bỏ qua.</summary>
    public void Start()
    {
        if (IsRunning) return;

        IsRunning = true;
        _subscription = _ticker.Schedule(TimeSpan.FromMilliseconds(ScanIntervalMs), OnTickSafely);
        _logger.Info($"Timer bắt đầu quét mỗi {ScanIntervalMs / 1000} giây.", "StatusUpdaterTimer");
    }

    /// <summary>Bảng 29 — dừng quét (dùng khi bảo trì hoặc tắt hệ thống).</summary>
    public void Stop()
    {
        _subscription?.Dispose();
        _subscription = null;
        IsRunning = false;
        _logger.Info("Timer đã dừng.", "StatusUpdaterTimer");
    }

    /// <summary>Chạy đúng một chu kỳ ngay lập tức (demo/test, ngoài Sub 3 — DC-06).</summary>
    public void TickNow() => OnTickSafely();

    /// <summary>Header "Chu kỳ quét 30s · Cập nhật N giây trước".</summary>
    public int GetSecondsSinceLastTick()
    {
        if (LastTickAt is null) return ScanIntervalMs / 1000;
        return Math.Max(0, (int)Math.Round((_clock.Now - LastTickAt.Value).TotalSeconds));
    }

    public int GetNextTickInSeconds() => Math.Max(0, ScanIntervalMs / 1000 - GetSecondsSinceLastTick());

    /// <summary>6 bước tuần tự — mỗi bước bọc try/catch riêng.</summary>
    private void OnTick()
    {
        RunStep("1. Đồng bộ trạng thái Hub", () =>
        {
            var synced = _hubSync.SyncHubStatus();
            _logger.Info($"Đồng bộ thành công {synced}/{_store.Hubs.Count} Hub.", "UC-SYS-02");
        });

        RunStep("2. Kiểm tra dữ liệu cũ", () =>
        {
            foreach (var hub in _store.Hubs.ToList()) CheckStaleData(hub);
        });

        RunStep("3. Kiểm tra ngưỡng quá tải", () =>
        {
            foreach (var alert in _thresholdMonitor.CheckAllHubs())
                _retryQueue.DispatchAlert(alert);
        });

        RunStep("4. Lập lịch sạc thông minh", () =>
        {
            foreach (var hub in _store.Hubs.Where(h => h.Status == HubStatus.Online && h.Connection == ConnectionStatus.Online).ToList())
                _smartScheduling.TriggerFor(hub);
        });

        RunStep("5. Huỷ đặt chỗ quá hạn", () =>
        {
            var cancelled = _autoCancel.AutoCancelExpiredReservations();
            if (cancelled > 0) _logger.Info($"Đã huỷ {cancelled} đặt chỗ quá hạn.", "UC-SYS-04");
        });

        RunStep("6. Gửi lại cảnh báo", () =>
        {
            var sent = _retryQueue.ProcessQueue(_clock.Now);
            if (sent > 0) _logger.Info($"Đã gửi lại {sent} cảnh báo.", "UC-SYS-03");
        });

        LastTickAt = _clock.Now;
    }

    /// <summary>
    /// Kiểm tra dữ liệu cũ của một Hub (Bảng 29). Trả true nếu dữ liệu quá 10 phút.
    /// Cảnh báo chỉ phát ở lần đầu phát hiện trong một đợt; dữ liệu mới lại thì đợt kết thúc.
    /// Đúng biên: 10 phút chưa tính là cũ.
    /// </summary>
    public bool CheckStaleData(MobilityHub hub)
    {
        if (hub is null) throw new IllegalArgumentException("Hub không hợp lệ.");

        var stale = (_clock.Now - hub.LastUpdatedAt).TotalMinutes > StaleThresholdMin;
        if (!stale)
        {
            _staleAlertedHubs.Remove(hub.Id);
            return false;
        }

        if (!_staleAlertedHubs.Add(hub.Id)) return true;

        var alert = new Alert
        {
            AlertId = _store.NextAlertId(),
            Type = AlertType.StaleData,
            HubId = hub.Id,
            Message = $"Dữ liệu {hub.ShortName} đã cũ hơn {StaleThresholdMin} phút.",
            CreatedAt = _clock.Now
        };

        _retryQueue.DispatchAlert(alert);
        return true;
    }

    private void OnTickSafely()
    {
        try
        {
            OnTick();
        }
        catch (Exception ex)
        {
            // Lưới an toàn cuối cùng: OnTick đã cách ly từng bước, đây chỉ chặn lỗi bất ngờ.
            _logger.Error($"Chu kỳ quét thất bại: {ex.Message}", "StatusUpdaterTimer");
        }
    }

    private void RunStep(string step, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _logger.Error($"Bước '{step}' thất bại: {ex.Message}", "StatusUpdaterTimer");
        }
    }
}
