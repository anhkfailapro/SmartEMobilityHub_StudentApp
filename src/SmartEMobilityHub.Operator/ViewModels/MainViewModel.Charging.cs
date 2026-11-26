using System.Globalization;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>
/// UC-OP-04 — Quản lý lịch sạc thông minh: chọn Hub, xem hàng chờ và công suất,
/// chỉnh trọng số ưu tiên (pin / thời điểm cần / cao điểm), lập lịch lại và ghim thứ tự thủ công.
/// </summary>
public sealed partial class MainViewModel
{
    private const int QueueRowsShown = 12;

    private string _chargingHubId = OperatorDemo.ChargingDemoHubId;
    private QueueRow? _selectedQueueRow;
    private string _chargingSummaryText = "";
    private string _weightsInfoText = "";
    private string _weightBatteryText = "";
    private string _weightNeedTimeText = "";
    private string _weightPeakText = "";

    public string ChargingHubId
    {
        get => _chargingHubId;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !SetProperty(ref _chargingHubId, value)) return;
            RefreshQueue();
            RefreshChargingPorts();
        }
    }

    public QueueRow? SelectedQueueRow
    {
        get => _selectedQueueRow;
        set
        {
            if (!SetProperty(ref _selectedQueueRow, value)) return;
            OnPropertyChanged(nameof(CanMoveQueueUp));
            OnPropertyChanged(nameof(CanMoveQueueDown));
        }
    }

    public string ChargingSummaryText { get => _chargingSummaryText; private set => SetProperty(ref _chargingSummaryText, value); }
    public string WeightsInfoText { get => _weightsInfoText; private set => SetProperty(ref _weightsInfoText, value); }
    public string WeightBatteryText { get => _weightBatteryText; set => SetProperty(ref _weightBatteryText, value); }
    public string WeightNeedTimeText { get => _weightNeedTimeText; set => SetProperty(ref _weightNeedTimeText, value); }
    public string WeightPeakText { get => _weightPeakText; set => SetProperty(ref _weightPeakText, value); }

    public bool CanMoveQueueUp => WaitingIndex(_selectedQueueRow) > 0;

    public bool CanMoveQueueDown
    {
        get
        {
            var index = WaitingIndex(_selectedQueueRow);
            return index >= 0 && index < WaitingQueue().Count - 1;
        }
    }

    /// <summary>Đổi trọng số ưu tiên (Bảng 37 UpdatePriorityRules) rồi lập lịch lại Hub đang chọn.</summary>
    public string? ApplyPriorityWeights()
    {
        if (!TryParseWeight(WeightBatteryText, out var battery) ||
            !TryParseWeight(WeightNeedTimeText, out var needTime) ||
            !TryParseWeight(WeightPeakText, out var peak))
        {
            return "Trọng số phải là số, ví dụ 0.5 hoặc 0,5.";
        }

        try
        {
            _demo.Charging.UpdatePriorityRules(battery, needTime, peak);
        }
        catch (IllegalArgumentException ex)
        {
            SyncWeightInputs();
            return ex.Message;
        }

        var plan = RescheduleSelectedHub();
        Note($"Đã áp dụng trọng số pin {battery:0.##} · thời điểm cần {needTime:0.##} · cao điểm {peak:0.##}; " +
             $"lập lịch lại {ChargingHubId}, cấp {plan} cổng.");
        Refresh();
        return null;
    }

    public void ResetPriorityWeights()
    {
        _demo.Charging.UpdatePriorityRules(
            SmartChargingScheduler.DefaultWeightBattery,
            SmartChargingScheduler.DefaultWeightNeedTime,
            SmartChargingScheduler.DefaultWeightPeak);
        RescheduleSelectedHub();
        Note("Đã khôi phục trọng số mặc định 0.50 / 0.35 / 0.15.");
        Refresh();
    }

    public void RescheduleCharging()
    {
        var ports = RescheduleSelectedHub();
        Note($"Đã lập lịch lại {ChargingHubId} · cấp {ports} cổng trong giới hạn công suất.");
        Refresh();
    }

    /// <summary>Ghim một yêu cầu lên/xuống một bậc (Bảng 37 ManualReorder). Yêu cầu đã ghim không bị tính lại điểm.</summary>
    public string? MoveSelectedQueueRow(int delta)
    {
        var row = _selectedQueueRow;
        var index = WaitingIndex(row);
        if (row is null || index < 0) return "Chọn một yêu cầu đang chờ cổng trong hàng chờ.";

        if (!_demo.Charging.ManualReorder(row.RequestId, index + delta))
            return "Không thể đổi vị trí yêu cầu này.";

        RefreshQueue();
        SelectedQueueRow = ChargingQueue.FirstOrDefault(r => r.RequestId == row.RequestId);
        // Báo đúng vị trí đang hiển thị (tính cả các xe đã được cấp cổng ở đầu danh sách).
        Note($"Đã ghim {row.Vehicle} ở vị trí {SelectedQueueRow?.PositionText ?? $"#{index + delta + 1}"} trong hàng chờ {ChargingHubId}.");
        return null;
    }

    private int RescheduleSelectedHub()
    {
        var hub = _demo.Store.FindHub(ChargingHubId);
        if (hub is null) return 0;

        var plan = _demo.Charging.RunSchedule(hub);
        return plan.OrderedRequests.Count(r => r.AssignedPortId is not null);
    }

    private void RefreshQueue()
    {
        var selectedId = _selectedQueueRow?.RequestId;
        ChargingQueue.Clear();

        var queue = _demo.Charging.GetQueue(ChargingHubId);
        var index = 0;
        foreach (var request in queue.Take(QueueRowsShown))
        {
            ChargingQueue.Add(new QueueRow(request, index));
            index++;
        }

        SelectedQueueRow = ChargingQueue.FirstOrDefault(r => r.RequestId == selectedId);

        var hub = _demo.Store.FindHub(ChargingHubId);
        if (hub is null)
        {
            ChargingSummaryText = "Chưa chọn Hub.";
        }
        else
        {
            var busy = hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Charging);
            var free = hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Available);
            var waiting = queue.Count(r => r.AssignedPortId is null);
            ChargingSummaryText =
                $"{hub.ShortName} · {busy} đang sạc · {free} cổng trống · {waiting} xe chờ · " +
                $"{_demo.Charging.GetUsedPowerKw(hub):0.#}/{hub.PowerLimitKw:0.#} kW";
        }

        WeightsInfoText =
            $"Đang dùng: pin {_demo.Charging.WeightBattery:0.##} · thời điểm cần {_demo.Charging.WeightNeedTime:0.##} · " +
            $"cao điểm {_demo.Charging.WeightPeak:0.##}";

        if (string.IsNullOrEmpty(_weightBatteryText)) SyncWeightInputs();
        OnPropertyChanged(nameof(CanMoveQueueUp));
        OnPropertyChanged(nameof(CanMoveQueueDown));
    }

    private void SyncWeightInputs()
    {
        WeightBatteryText = _demo.Charging.WeightBattery.ToString("0.##", CultureInfo.InvariantCulture);
        WeightNeedTimeText = _demo.Charging.WeightNeedTime.ToString("0.##", CultureInfo.InvariantCulture);
        WeightPeakText = _demo.Charging.WeightPeak.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private List<ChargingRequest> WaitingQueue() =>
        _demo.Charging.GetQueue(ChargingHubId).Where(r => r.AssignedPortId is null).ToList();

    private int WaitingIndex(QueueRow? row) =>
        row is null || !row.IsWaiting ? -1 : WaitingQueue().FindIndex(r => r.RequestId == row.RequestId);

    /// <summary>Nhận cả "0.5" và "0,5" (máy cài tiếng Việt dùng dấu phẩy thập phân).</summary>
    private static bool TryParseWeight(string? text, out double value) =>
        double.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
