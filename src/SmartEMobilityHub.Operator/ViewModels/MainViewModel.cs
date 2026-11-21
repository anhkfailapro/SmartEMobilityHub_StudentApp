using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

public sealed class MainViewModel : ObservableObject
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

    public FilterOption SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (value is null || !SetProperty(ref _selectedFilter, value)) return;
            Refresh();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            Refresh();
        }
    }

    public IncidentRow? SelectedIncident
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value)) return;
            _selected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(DetailVisibility));
            OnPropertyChanged(nameof(EmptyVisibility));
            RefreshSelectedTasks();
            RefreshTechnicians();
            RefreshActionFlags();
        }
    }

    public bool HasSelection => _selected is not null;
    public Visibility DetailVisibility => HasSelection ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyVisibility => HasSelection ? Visibility.Collapsed : Visibility.Visible;

    public bool CanLock => _selected?.StatusRaw is IncidentStatus.New or IncidentStatus.UnderCheck;
    public bool CanCreateTask => _selected is not null && _selected.StatusRaw != IncidentStatus.Closed;
    public bool CanAssign => _selected?.StatusRaw is IncidentStatus.New or IncidentStatus.UnderCheck or IncidentStatus.Locked or IncidentStatus.InRepair;
    public bool CanResolve => _selected?.StatusRaw == IncidentStatus.InRepair;
    public bool CanClose => _selected?.StatusRaw == IncidentStatus.Resolved;
    public bool CanSuspend
    {
        get
        {
            var hub = _demo.Store.FindHub(_selected?.HubId ?? "");
            return hub is not null && hub.Status != HubStatus.Suspended;
        }
    }

    /// <summary>Nhịp UI 1 giây: thời gian mô phỏng trôi, tự quét mỗi 30 giây.</summary>
    public void Tick()
    {
        _demo.Clock.AdvanceSeconds(SpeedUp ? 10 : 1);

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
    }

    // ---------- Hành động UC-OP-03 ----------

    public string? LockSelected()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";
        var code = _selected.Code;

        try
        {
            _demo.Incidents.LockDevice(_selected.Id);
            Note($"Đã tạm khoá thiết bị {code}.");
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException)
        {
            return ex.Message;
        }

        Refresh();
        return null;
    }

    public string? CreateTaskSelected()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        try
        {
            var task = _demo.Incidents.CreateMaintenanceTask(_selected.Id);
            Note($"Đã tạo phiếu {task.TaskId} · hạn SLA {task.SlaDeadline:HH:mm}.");
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException)
        {
            return ex.Message;
        }

        Refresh();
        return null;
    }

    public string? AssignSelected()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";
        var incident = _selected;

        try
        {
            var task = _demo.Store.MaintenanceTasks.FirstOrDefault(t =>
                           t.IncidentId == incident.Id &&
                           t.Status is MaintenanceStatus.Assigned or MaintenanceStatus.InProgress)
                       ?? _demo.Incidents.CreateMaintenanceTask(incident.Id);

            var technician = _demo.Incidents.SuggestTechnician(incident.HubId);
            if (technician is null) return "Không có kỹ thuật viên nào đang sẵn sàng.";

            if (!_demo.Incidents.AssignTechnician(task.TaskId, technician.TechId))
                return "Không gán được kỹ thuật viên cho phiếu này.";

            var hub = _demo.Store.FindHub(incident.HubId);
            var distance = hub is null ? "" : $" · {technician.DistanceToHub(hub):0.0} km";
            Note($"Đã gán {technician.Name} ({technician.Team}){distance}.");
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException or IllegalArgumentException)
        {
            return ex.Message;
        }

        Refresh();
        return null;
    }

    public string? ResolveSelected()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        try
        {
            _demo.Incidents.UpdateIncidentStatus(_selected.Id, IncidentStatus.Resolved);
            Note($"Đã đánh dấu {_selected.Id} là đã xử lý.");
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException)
        {
            return ex.Message;
        }

        Refresh();
        return null;
    }

    public string? CloseSelected()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        try
        {
            _demo.Incidents.UpdateIncidentStatus(_selected.Id, IncidentStatus.Closed);
            Note($"Đã đóng sự cố {_selected.Id}.");
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException)
        {
            return ex.Message;
        }

        Refresh();
        return null;
    }

    public string? SuspendSelectedHub()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        var hubId = _selected.HubId;
        if (string.IsNullOrWhiteSpace(hubId)) return "Sự cố không gắn với Hub nào.";

        try
        {
            var cancelled = _demo.Incidents.SuspendHub(hubId, "OP-01");
            Note($"{hubId} đã tạm dừng · huỷ {cancelled} đặt chỗ sắp tới.");
        }
        catch (Exception ex) when (ex is HubNotFoundException or IllegalArgumentException)
        {
            return ex.Message;
        }

        Refresh();
        return null;
    }

    // ---------- Demo panel ----------

    public void BreakPort()
    {
        _demo.BreakChargingPoint("CS-08-08", "HUB-08");
        Note("Đã bơm lỗi cổng CS-08-08 · sinh viên/cảm biến đã tạo sự cố.");
        Refresh();
    }

    public void DisconnectHub()
    {
        _demo.DisconnectHub("HUB-03");
        Note("Đã ngắt cảm biến HUB-03 · cả 4 cổng chuyển OFFLINE.");
        Refresh();
    }

    public void MakeStale()
    {
        _demo.MakeStale("HUB-08");
        Note("Dữ liệu HUB-08 đã cũ 14 phút · cảnh báo STALE_DATA đã phát.");
        Refresh();
    }

    public void BreakChannel()
    {
        var pending = _demo.BreakAlertChannel();
        Note($"Mạng cảnh báo đang lỗi · {pending} cảnh báo nằm trong hàng đợi gửi lại.");
        Refresh();
    }

    public void RestoreChannel()
    {
        var sent = _demo.RestoreAlertChannel();
        Note($"Đã khôi phục mạng · gửi lại thành công {sent} cảnh báo.");
        Refresh();
    }

    public void RaiseUnlockIncident()
    {
        var record = _demo.RaiseUnlockTimeout();
        Note($"Đã tạo {record.IncidentId} · xe XE-121 giữ RESERVED, đơn R-103 bị huỷ.");
        Refresh();
    }

    public void AddChargingRequests()
    {
        var ports = _demo.AddChargingRequests();
        Note($"Đã lập lịch sạc HUB-08 · cấp {ports} cổng trong giới hạn công suất.");
        Refresh();
    }

    // ---------- Nội bộ ----------

    private bool Matches(IncidentRecord incident)
    {
        if (!_selectedFilter.Matches(incident.Status)) return false;
        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        var query = _searchText.Trim();
        return incident.IncidentId.Contains(query, StringComparison.OrdinalIgnoreCase)
               || incident.ObjectCode.Contains(query, StringComparison.OrdinalIgnoreCase)
               || incident.ProblemType.Contains(query, StringComparison.OrdinalIgnoreCase)
               || incident.HubId.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshSelectedTasks()
    {
        SelectedTasks.Clear();
        if (_selected is null) return;

        foreach (var task in _demo.Store.MaintenanceTasks.Where(t => t.IncidentId == _selected.Id))
            SelectedTasks.Add(new MaintenanceRow(task, _demo.Clock.Now));
    }

    private void RefreshTechnicians()
    {
        Technicians.Clear();
        var hub = _demo.Store.FindHub(_selected?.HubId ?? "");

        foreach (var technician in _demo.Store.Technicians
                     .OrderByDescending(t => t.IsAvailable())
                     .ThenBy(t => t.TechId, StringComparer.Ordinal))
        {
            Technicians.Add(new TechnicianRow(technician, hub));
        }
    }

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

    private void RefreshQueue()
    {
        ChargingQueue.Clear();
        var index = 0;

        foreach (var request in _demo.Charging.GetQueue("HUB-08").Take(6))
        {
            ChargingQueue.Add(new QueueRow(request, index));
            index++;
        }
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

    private void RefreshActionFlags()
    {
        OnPropertyChanged(nameof(CanLock));
        OnPropertyChanged(nameof(CanCreateTask));
        OnPropertyChanged(nameof(CanAssign));
        OnPropertyChanged(nameof(CanResolve));
        OnPropertyChanged(nameof(CanClose));
        OnPropertyChanged(nameof(CanSuspend));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(DetailVisibility));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    private void Note(string message) => LastActionText = message;
}
