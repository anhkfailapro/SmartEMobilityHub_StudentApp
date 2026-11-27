using System.Collections.ObjectModel;
using System.Globalization;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>
/// Trang "Lịch sạc thông minh" (UC-SYS-01 / UC-OP-04): chọn Hub, xem hàng chờ và công suất,
/// chỉnh trọng số ưu tiên (pin / thời điểm cần / cao điểm), lập lịch lại và ghim thứ tự thủ công.
/// </summary>
public sealed class ChargingPageViewModel : PageViewModel
{
    private const int QueueRowsShown = 12;

    private string _chargingHubId = OperatorDemo.ChargingDemoHubId;
    private QueueRow? _selectedQueueRow;
    private HubPortGroup? _chargingHubPorts;
    private string _chargingSummaryText = "";
    private string _weightsInfoText = "";
    private string _weightBatteryText = "";
    private string _weightNeedTimeText = "";
    private string _weightPeakText = "";

    private RelayCommand? _queueUpCommand;
    private RelayCommand? _queueDownCommand;
    private RelayCommand? _rescheduleCommand;
    private RelayCommand? _applyWeightsCommand;
    private RelayCommand? _resetWeightsCommand;
    private RelayCommand? _addChargingRequestsCommand;

    public ChargingPageViewModel(DashboardContext shell) : base(shell)
    {
    }

    public ObservableCollection<string> HubIds { get; } = new();
    public ObservableCollection<QueueRow> ChargingQueue { get; } = new();

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

    /// <summary>Lưới cổng sạc của Hub đang chọn.</summary>
    public HubPortGroup? ChargingHubPorts { get => _chargingHubPorts; private set => SetProperty(ref _chargingHubPorts, value); }

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

    // ---------- Lệnh ----------
    public RelayCommand QueueUpCommand => _queueUpCommand ??= new(() => Run(() => MoveSelectedQueueRow(-1)));
    public RelayCommand QueueDownCommand => _queueDownCommand ??= new(() => Run(() => MoveSelectedQueueRow(1)));
    public RelayCommand RescheduleCommand => _rescheduleCommand ??= new(RescheduleCharging);
    public RelayCommand ApplyWeightsCommand => _applyWeightsCommand ??= new(() => Run(ApplyPriorityWeights));
    public RelayCommand ResetWeightsCommand => _resetWeightsCommand ??= new(ResetPriorityWeights);
    public RelayCommand AddChargingRequestsCommand => _addChargingRequestsCommand ??= new(AddChargingRequests);

    public override void Refresh()
    {
        var currentIds = Store.Hubs.Select(h => h.Id).ToList();
        if (!HubIds.SequenceEqual(currentIds))
        {
            HubIds.Clear();
            foreach (var id in currentIds) HubIds.Add(id);
        }

        RefreshQueue();
        RefreshChargingPorts();
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
            Session.Charging.UpdatePriorityRules(battery, needTime, peak);
        }
        catch (IllegalArgumentException ex)
        {
            SyncWeightInputs();
            return ex.Message;
        }

        var plan = RescheduleSelectedHub();
        Note($"Đã áp dụng trọng số pin {battery:0.##} · thời điểm cần {needTime:0.##} · cao điểm {peak:0.##}; " +
             $"lập lịch lại {ChargingHubId}, cấp {plan} cổng.");
        RefreshAll();
        return null;
    }

    public void ResetPriorityWeights()
    {
        Session.Charging.UpdatePriorityRules(
            SmartChargingScheduler.DefaultWeightBattery,
            SmartChargingScheduler.DefaultWeightNeedTime,
            SmartChargingScheduler.DefaultWeightPeak);
        RescheduleSelectedHub();
        SyncWeightInputs();
        Note("Đã khôi phục trọng số mặc định 0.50 / 0.35 / 0.15.");
        RefreshAll();
    }

    public void RescheduleCharging()
    {
        var ports = RescheduleSelectedHub();
        Note($"Đã lập lịch lại {ChargingHubId} · cấp {ports} cổng trong giới hạn công suất.");
        RefreshAll();
    }

    /// <summary>Kịch bản demo: 3 yêu cầu sạc mẫu tại Hub demo, mở luôn Hub đó trên trang.</summary>
    public void AddChargingRequests()
    {
        var ports = Shell.Demo.AddChargingRequests();
        ChargingHubId = OperatorDemo.ChargingDemoHubId;
        Note($"Đã lập lịch sạc {OperatorDemo.ChargingDemoHubId} · cấp {ports} cổng trong giới hạn công suất.");
        RefreshAll();
    }

    /// <summary>Ghim một yêu cầu lên/xuống một bậc (Bảng 37 ManualReorder). Yêu cầu đã ghim không bị tính lại điểm.</summary>
    public string? MoveSelectedQueueRow(int delta)
    {
        var row = _selectedQueueRow;
        var index = WaitingIndex(row);
        if (row is null || index < 0) return "Chọn một yêu cầu đang chờ cổng trong hàng chờ.";

        if (!Session.Charging.ManualReorder(row.RequestId, index + delta))
            return "Không thể đổi vị trí yêu cầu này.";

        RefreshQueue();
        SelectedQueueRow = ChargingQueue.FirstOrDefault(r => r.RequestId == row.RequestId);
        // Báo đúng vị trí đang hiển thị (tính cả các xe đã được cấp cổng ở đầu danh sách).
        Note($"Đã ghim {row.Vehicle} ở vị trí {SelectedQueueRow?.PositionText ?? $"#{index + delta + 1}"} trong hàng chờ {ChargingHubId}.");
        return null;
    }

    // ---------- Nội bộ ----------

    private int RescheduleSelectedHub()
    {
        var hub = Store.FindHub(ChargingHubId);
        if (hub is null) return 0;

        var plan = Session.Charging.RunSchedule(hub);
        return plan.OrderedRequests.Count(r => r.AssignedPortId is not null);
    }

    private void RefreshQueue()
    {
        var selectedId = _selectedQueueRow?.RequestId;
        ChargingQueue.Clear();

        var queue = Session.Charging.GetQueue(ChargingHubId);
        var index = 0;
        foreach (var request in queue.Take(QueueRowsShown))
        {
            ChargingQueue.Add(new QueueRow(request, index));
            index++;
        }

        SelectedQueueRow = ChargingQueue.FirstOrDefault(r => r.RequestId == selectedId);

        var hub = Store.FindHub(ChargingHubId);
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
                $"{Session.Charging.GetUsedPowerKw(hub):0.#}/{hub.PowerLimitKw:0.#} kW";
        }

        WeightsInfoText =
            $"Đang dùng: pin {Session.Charging.WeightBattery:0.##} · thời điểm cần {Session.Charging.WeightNeedTime:0.##} · " +
            $"cao điểm {Session.Charging.WeightPeak:0.##}";

        if (string.IsNullOrEmpty(_weightBatteryText)) SyncWeightInputs();
        OnPropertyChanged(nameof(CanMoveQueueUp));
        OnPropertyChanged(nameof(CanMoveQueueDown));
    }

    private void RefreshChargingPorts()
    {
        var hub = Store.FindHub(ChargingHubId);
        ChargingHubPorts = hub is null ? null : new HubPortGroup(hub);
    }

    private void SyncWeightInputs()
    {
        WeightBatteryText = Session.Charging.WeightBattery.ToString("0.##", CultureInfo.InvariantCulture);
        WeightNeedTimeText = Session.Charging.WeightNeedTime.ToString("0.##", CultureInfo.InvariantCulture);
        WeightPeakText = Session.Charging.WeightPeak.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private List<ChargingRequest> WaitingQueue() =>
        Session.Charging.GetQueue(ChargingHubId).Where(r => r.AssignedPortId is null).ToList();

    private int WaitingIndex(QueueRow? row) =>
        row is null || !row.IsWaiting ? -1 : WaitingQueue().FindIndex(r => r.RequestId == row.RequestId);

    /// <summary>Nhận cả "0.5" và "0,5" (máy cài tiếng Việt dùng dấu phẩy thập phân).</summary>
    private static bool TryParseWeight(string? text, out double value) =>
        double.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
