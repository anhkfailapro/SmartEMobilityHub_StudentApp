using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// UC-SYS-03 — đối chiếu mức sử dụng với ngưỡng (mặc định 0,90) và sinh cảnh báo quá tải
/// (Sub3 Bảng 30/31). Không xét Hub đang OFFLINE/SUSPENDED.
/// </summary>
public sealed class ThresholdMonitor
{
    public const double DefaultThreshold = 0.90;

    private readonly NetworkStore _store;
    private readonly IClock _clock;
    private readonly ISystemLogger _logger;

    public ThresholdMonitor(NetworkStore store, IClock clock, ISystemLogger logger)
    {
        _store = store;
        _clock = clock;
        _logger = logger;
    }

    public double Threshold { get; private set; } = DefaultThreshold;

    /// <summary>null nếu chưa chạm ngưỡng (Bảng 31).</summary>
    public Alert? EvaluateHub(MobilityHub hub)
    {
        if (hub.UsableChargingPorts <= 0 && hub.ParkingCapacity <= 0)
        {
            _logger.Warn($"Bỏ qua {hub.Id}: không có sức chứa hợp lệ.", "UC-SYS-03");
            return null;
        }

        var utilization = hub.MaxUtilization;
        if (utilization < Threshold) return null;

        return new Alert
        {
            AlertId = _store.NextAlertId(),
            Type = AlertType.HubOverload,
            HubId = hub.Id,
            Message = $"{hub.ShortName} đạt {utilization * 100:0}% sức chứa ({hub.OccupiedParkingSlots}/{hub.ParkingCapacity} chỗ) — nguy cơ quá tải.",
            CreatedAt = _clock.Now
        };
    }

    /// <summary>Bảng 31: duyệt toàn bộ Hub, bỏ Hub OFFLINE/SUSPENDED.</summary>
    public List<Alert> CheckAllHubs(IEnumerable<MobilityHub>? hubs = null)
    {
        var alerts = new List<Alert>();
        var source = hubs ?? _store.Hubs;

        foreach (var hub in source.ToList())
        {
            if (hub.Status != HubStatus.Online || hub.Connection != ConnectionStatus.Online)
            {
                _logger.Info($"Bỏ qua {hub.Id} vì Hub không ONLINE.", "UC-SYS-03");
                continue;
            }

            var alert = EvaluateHub(hub);
            if (alert is not null) alerts.Add(alert);
        }

        return alerts;
    }

    /// <summary>Bảng 31: ngưỡng phải nằm trong (0, 1].</summary>
    public void SetThreshold(double value)
    {
        if (value <= 0 || value > 1)
            throw new IllegalArgumentException($"Ngưỡng phải trong khoảng (0, 1], nhận được {value}.");

        Threshold = value;
    }

    /// <summary>Bảng 31 — biên: 54/60 = 90% là quá tải, 53/60 = 88,3% thì không.</summary>
    public bool IsOverloaded(int used, int capacity) => capacity > 0 && used >= Threshold * capacity;
}
