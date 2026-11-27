using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Models;
using SmartEMobilityHub.Core.Services;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>
/// ViewModel của Trung tâm điều hành, chia theo trang (DC-52): file này giữ phần chung (đồng hồ, chu kỳ quét,
/// Refresh, header, KPI); .Network (Tổng quan, Xe &amp; cổng sạc, điều hướng), .Incidents, .Charging, .Dispatch,
/// .Demo và .Commands (lệnh cho các nút).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly OperatorDemo _demo;

    private FilterOption _selectedFilter;
    private IncidentRow? _selected;
    private string _searchText = "";
    private string _lastAction = "Sẵn sàng — bấm “Quét ngay” hoặc để tự động quét mỗi 30 giây.";
    private string _clockText = "";
    private string _scanText = "";
    private string _kpiNew = "0";
    private string _kpiCheck = "0";
    private string _kpiRepair = "0";
    private string _kpiDone = "0";
    private string _overloadText = "0";
    private string _alertCountText = "0";
    private string _techText = "0/0";
    private string _visibleCountText = "0 sự cố";
    private bool _autoScan = true;
    private bool _speedUp;

    public MainViewModel(OperatorDemo demo)
    {
        _demo = demo;
        Filters = new ObservableCollection<FilterOption>
        {
            new("Tất cả"),
            new("Mới", IncidentStatus.New),
            new("Kiểm tra", IncidentStatus.UnderCheck, IncidentStatus.Locked),
            new("Đang sửa", IncidentStatus.InRepair),
            new("Đã xong", IncidentStatus.Resolved, IncidentStatus.Closed)
        };
        _selectedFilter = Filters[0];
        Refresh();
    }

    public ObservableCollection<FilterOption> Filters { get; }
    public ObservableCollection<IncidentRow> Incidents { get; } = new();
    public ObservableCollection<MaintenanceRow> SelectedTasks { get; } = new();
    public ObservableCollection<TechnicianRow> Technicians { get; } = new();
    public ObservableCollection<AlertRow> Alerts { get; } = new();
    public ObservableCollection<QueueRow> ChargingQueue { get; } = new();
    public ObservableCollection<string> Logs => _demo.Logger.Lines;

    public string ClockText
    {
        get => _clockText;
        private set => SetProperty(ref _clockText, value);
    }

    public string ScanText
    {
        get => _scanText;
        private set => SetProperty(ref _scanText, value);
    }

    public string LastActionText
    {
        get => _lastAction;
        private set => SetProperty(ref _lastAction, value);
    }

    public string KpiNew { get => _kpiNew; private set => SetProperty(ref _kpiNew, value); }
    public string KpiCheck { get => _kpiCheck; private set => SetProperty(ref _kpiCheck, value); }
    public string KpiRepair { get => _kpiRepair; private set => SetProperty(ref _kpiRepair, value); }
    public string KpiDone { get => _kpiDone; private set => SetProperty(ref _kpiDone, value); }
    public string OverloadText { get => _overloadText; private set => SetProperty(ref _overloadText, value); }
    public string AlertCountText { get => _alertCountText; private set => SetProperty(ref _alertCountText, value); }
    public string TechText { get => _techText; private set => SetProperty(ref _techText, value); }
    public string VisibleCountText { get => _visibleCountText; private set => SetProperty(ref _visibleCountText, value); }

    public bool AutoScan { get => _autoScan; set => SetProperty(ref _autoScan, value); }
    public bool SpeedUp { get => _speedUp; set => SetProperty(ref _speedUp, value); }

    /// <summary>Nhịp UI 1 giây: thời gian mô phỏng trôi, tự quét mỗi 30 giây.</summary>
    public void Tick()
    {
        // Đồng hồ mô phỏng tự trôi theo giờ thật (DC-54); chế độ ×10 tua thêm 9 giây mỗi giây.
        if (SpeedUp) _demo.Clock.AdvanceSeconds(9);

        var lastTick = _demo.Timer.LastTickAt;
        var due = lastTick is null || (_demo.Clock.Now - lastTick.Value).TotalSeconds >= 30;

        if (AutoScan && due)
        {
            _demo.Scan();
            Refresh();
            return;
        }

        UpdateHeader();
    }

    public void ScanNow()
    {
        _demo.Scan();
        Note("Đã chạy chu kỳ quét 6 bước.");
        Refresh();
    }

    public void Advance(int minutes)
    {
        _demo.Advance(minutes);
        Note($"Đã tua +{minutes} phút. Bấm “Quét ngay” để áp dụng chu kỳ.");
        Refresh();
    }

    public void Refresh()
    {
        var previousId = _selected?.Id;
        var all = _demo.Incidents.GetIncidentQueue();
        var visible = all.Where(Matches).ToList();

        Incidents.Clear();
        foreach (var incident in visible)
            Incidents.Add(new IncidentRow(incident, _demo.Store, _demo.Clock.Now));

        SelectedIncident = Incidents.FirstOrDefault(r => r.Id == previousId) ?? Incidents.FirstOrDefault();
        RefreshSelectedTasks();

        KpiNew = all.Count(i => i.Status == IncidentStatus.New).ToString();
        KpiCheck = all.Count(i => i.Status is IncidentStatus.UnderCheck or IncidentStatus.Locked).ToString();
        KpiRepair = all.Count(i => i.Status == IncidentStatus.InRepair).ToString();
        KpiDone = all.Count(i => i.Status is IncidentStatus.Resolved or IncidentStatus.Closed).ToString();
        VisibleCountText = $"{Incidents.Count} sự cố";
        OnPropertyChanged(nameof(EmptyHintText));

        OverloadText = _demo.Store.Hubs.Count(h =>
            h.Status == HubStatus.Online &&
            h.Connection == ConnectionStatus.Online &&
            h.MaxUtilization >= _demo.Threshold.Threshold).ToString();

        TechText = $"{_demo.Store.Technicians.Count(t => t.IsAvailable())}/{_demo.Store.Technicians.Count}";

        RefreshTechnicians();
        RefreshAlerts();
        RefreshQueue();
        UpdateHeader();
        RefreshActionFlags();
        RefreshHubsAndDispatch();
        RefreshNetwork();
    }

    // ---------- Nội bộ ----------

    private void RefreshAlerts()
    {
        var merged = _demo.Notification.OperatorAlerts
            .Concat(_demo.RetryQueue.Pending)
            .GroupBy(a => a.AlertId)
            .Select(g => g.First())
            .OrderByDescending(a => a.CreatedAt)
            .Take(6)
            .ToList();

        Alerts.Clear();
        foreach (var alert in merged) Alerts.Add(new AlertRow(alert));
        AlertCountText = merged.Count.ToString();
    }

    private void UpdateHeader()
    {
        ClockText = _demo.Clock.Now.ToString("HH:mm:ss");

        ScanText = _demo.Timer.LastTickAt is null
            ? "Chưa quét · chu kỳ 30 giây"
            : $"Cập nhật {_demo.Timer.GetSecondsSinceLastTick()} giây trước · chu kỳ 30 giây";

        OnPropertyChanged(nameof(ClockText));
        OnPropertyChanged(nameof(ScanText));
    }

    private void Note(string message) => LastActionText = message;
}
