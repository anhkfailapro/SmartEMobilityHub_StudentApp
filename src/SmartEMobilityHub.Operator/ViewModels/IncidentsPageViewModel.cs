using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>Trang "Sự cố &amp; bảo trì" (UC-OP-03): lọc, chọn sự cố, các nút xử lý và cờ bật/tắt nút.</summary>
public sealed class IncidentsPageViewModel : PageViewModel
{
    private FilterOption _selectedFilter;
    private IncidentRow? _selected;
    private string _searchText = "";
    private string _kpiNew = "0";
    private string _kpiCheck = "0";
    private string _kpiRepair = "0";
    private string _kpiDone = "0";
    private string _techText = "0/0";
    private string _visibleCountText = "0 sự cố";

    private RelayCommand? _lockCommand;
    private RelayCommand? _createTaskCommand;
    private RelayCommand? _assignCommand;
    private RelayCommand? _resolveCommand;
    private RelayCommand? _closeIncidentCommand;
    private RelayCommand? _suspendHubCommand;
    private RelayCommand? _resumeHubCommand;

    public IncidentsPageViewModel(DashboardContext shell) : base(shell)
    {
        Filters = new ObservableCollection<FilterOption>
        {
            new("Tất cả"),
            new("Mới", IncidentStatus.New),
            new("Kiểm tra", IncidentStatus.UnderCheck, IncidentStatus.Locked),
            new("Đang sửa", IncidentStatus.InRepair),
            new("Đã xong", IncidentStatus.Resolved, IncidentStatus.Closed)
        };
        _selectedFilter = Filters[0];
    }

    public ObservableCollection<FilterOption> Filters { get; }
    public ObservableCollection<IncidentRow> Incidents { get; } = new();
    public ObservableCollection<MaintenanceRow> SelectedTasks { get; } = new();
    public ObservableCollection<TechnicianRow> Technicians { get; } = new();

    public string KpiNew { get => _kpiNew; private set => SetProperty(ref _kpiNew, value); }
    public string KpiCheck { get => _kpiCheck; private set => SetProperty(ref _kpiCheck, value); }
    public string KpiRepair { get => _kpiRepair; private set => SetProperty(ref _kpiRepair, value); }
    public string KpiDone { get => _kpiDone; private set => SetProperty(ref _kpiDone, value); }
    public string TechText { get => _techText; private set => SetProperty(ref _techText, value); }
    public string VisibleCountText { get => _visibleCountText; private set => SetProperty(ref _visibleCountText, value); }

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
            RefreshSelectedTasks();
            RefreshTechnicians();
            RefreshActionFlags();
        }
    }

    public bool HasSelection => _selected is not null;

    /// <summary>Tóm tắt theo dữ liệu thật (trước đây là chuỗi cố định "24 sự cố mẫu…", sai sau khi xử lý sự cố).</summary>
    public string EmptyHintText => $"{KpiNew} mới · {KpiCheck} kiểm tra / khoá · {KpiRepair} đang sửa · {KpiDone} đã xong";
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
            var hub = Store.FindHub(_selected?.HubId ?? "");
            return hub is not null && hub.Status != HubStatus.Suspended;
        }
    }

    public bool CanResume => Store.FindHub(_selected?.HubId ?? "")?.Status == HubStatus.Suspended;

    // ---------- Lệnh ----------
    public RelayCommand LockCommand => _lockCommand ??= new(() => Run(LockSelected));
    public RelayCommand CreateTaskCommand => _createTaskCommand ??= new(() => Run(CreateTaskSelected));
    public RelayCommand AssignCommand => _assignCommand ??= new(() => Run(AssignSelected));
    public RelayCommand ResolveCommand => _resolveCommand ??= new(() => Run(ResolveSelected));
    public RelayCommand CloseIncidentCommand => _closeIncidentCommand ??= new(() => Run(CloseSelected));
    public RelayCommand ResumeHubCommand => _resumeHubCommand ??= new(() => Run(ResumeSelectedHub));

    public RelayCommand SuspendHubCommand => _suspendHubCommand ??= new(() =>
    {
        if (SelectedIncident is null) return;
        var hubId = SelectedIncident.HubId;
        var confirmed = Shell.Dialogs?.Confirm("Xác nhận tạm dừng Hub",
            $"Tạm dừng {hubId}?\nToàn bộ đặt chỗ sắp tới tại Hub này sẽ bị huỷ và người dùng được thông báo.") ?? true;
        if (confirmed) Run(SuspendSelectedHub);
    });

    public override void Refresh()
    {
        var previousId = _selected?.Id;
        var all = Session.Incidents.GetIncidentQueue();

        Incidents.Clear();
        foreach (var incident in all.Where(Matches))
            Incidents.Add(new IncidentRow(incident, Store, Now));

        KpiNew = all.Count(i => i.Status == IncidentStatus.New).ToString();
        KpiCheck = all.Count(i => i.Status is IncidentStatus.UnderCheck or IncidentStatus.Locked).ToString();
        KpiRepair = all.Count(i => i.Status == IncidentStatus.InRepair).ToString();
        KpiDone = all.Count(i => i.Status is IncidentStatus.Resolved or IncidentStatus.Closed).ToString();
        VisibleCountText = $"{Incidents.Count} sự cố";
        TechText = Shell.TechnicianSummary();
        OnPropertyChanged(nameof(EmptyHintText));

        // Dòng mới được tạo lại mỗi lần làm mới: chọn lại theo mã rồi dựng lại phiếu, KTV và cờ nút.
        _selected = Incidents.FirstOrDefault(r => r.Id == previousId) ?? Incidents.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedIncident));
        RefreshSelectedTasks();
        RefreshTechnicians();
        RefreshActionFlags();
    }

    public string? LockSelected()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";
        var code = _selected.Code;

        try
        {
            Session.Incidents.LockDevice(_selected.Id);
            Note($"Đã tạm khoá thiết bị {code}.");
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException)
        {
            return ex.Message;
        }

        RefreshAll();
        return null;
    }

    public string? CreateTaskSelected()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        try
        {
            var task = Session.Incidents.CreateMaintenanceTask(_selected.Id);
            Note($"Đã tạo phiếu {task.TaskId} · hạn SLA {task.SlaDeadline:HH:mm}.");
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException)
        {
            return ex.Message;
        }

        RefreshAll();
        return null;
    }

    public string? AssignSelected()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";
        var incident = _selected;

        try
        {
            var task = Store.MaintenanceTasks.FirstOrDefault(t =>
                           t.IncidentId == incident.Id &&
                           t.Status is MaintenanceStatus.Assigned or MaintenanceStatus.InProgress)
                       ?? Session.Incidents.CreateMaintenanceTask(incident.Id);

            var technician = Session.Incidents.SuggestTechnician(incident.HubId);
            if (technician is null) return "Không có kỹ thuật viên nào đang sẵn sàng.";

            if (!Session.Incidents.AssignTechnician(task.TaskId, technician.TechId))
                return "Không gán được kỹ thuật viên cho phiếu này.";

            var hub = Store.FindHub(incident.HubId);
            var distance = hub is null ? "" : $" · {technician.DistanceToHub(hub):0.0} km";
            Note($"Đã gán {technician.Name} ({technician.Team}){distance}.");
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException or IllegalArgumentException)
        {
            return ex.Message;
        }

        RefreshAll();
        return null;
    }

    public string? ResolveSelected() => UpdateSelectedStatus(IncidentStatus.Resolved, id => $"Đã đánh dấu {id} là đã xử lý.");

    public string? CloseSelected() => UpdateSelectedStatus(IncidentStatus.Closed, id => $"Đã đóng sự cố {id}.");

    public string? SuspendSelectedHub()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        var hubId = _selected.HubId;
        if (string.IsNullOrWhiteSpace(hubId)) return "Sự cố không gắn với Hub nào.";

        try
        {
            var cancelled = Session.Incidents.SuspendHub(hubId, "OP-01");
            Note($"{hubId} đã tạm dừng · huỷ {cancelled} đặt chỗ sắp tới.");
        }
        catch (Exception ex) when (ex is HubNotFoundException or IllegalArgumentException)
        {
            return ex.Message;
        }

        RefreshAll();
        return null;
    }

    public string? ResumeSelectedHub()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        var hubId = _selected.HubId;
        if (string.IsNullOrWhiteSpace(hubId)) return "Sự cố không gắn với Hub nào.";

        try
        {
            if (!Session.Incidents.ResumeHub(hubId, "OP-01")) return $"{hubId} không ở trạng thái tạm dừng.";
            Note($"{hubId} đã mở lại hoạt động.");
        }
        catch (HubNotFoundException ex)
        {
            return ex.Message;
        }

        RefreshAll();
        return null;
    }

    // ---------- Nội bộ ----------

    private string? UpdateSelectedStatus(IncidentStatus status, Func<string, string> message)
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        try
        {
            Session.Incidents.UpdateIncidentStatus(_selected.Id, status);
            Note(message(_selected.Id));
        }
        catch (Exception ex) when (ex is IllegalStateException or IncidentNotFoundException)
        {
            return ex.Message;
        }

        RefreshAll();
        return null;
    }

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

        foreach (var task in Store.MaintenanceTasks.Where(t => t.IncidentId == _selected.Id))
            SelectedTasks.Add(new MaintenanceRow(task, Now));
    }

    private void RefreshTechnicians()
    {
        Technicians.Clear();
        var hub = Store.FindHub(_selected?.HubId ?? "");

        foreach (var technician in Store.Technicians
                     .OrderByDescending(t => t.IsAvailable())
                     .ThenBy(t => t.TechId, StringComparer.Ordinal))
        {
            Technicians.Add(new TechnicianRow(technician, hub));
        }
    }

    private void RefreshActionFlags()
    {
        OnPropertyChanged(nameof(CanLock));
        OnPropertyChanged(nameof(CanCreateTask));
        OnPropertyChanged(nameof(CanAssign));
        OnPropertyChanged(nameof(CanResolve));
        OnPropertyChanged(nameof(CanClose));
        OnPropertyChanged(nameof(CanSuspend));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(DetailVisibility));
        OnPropertyChanged(nameof(EmptyVisibility));
    }
}
