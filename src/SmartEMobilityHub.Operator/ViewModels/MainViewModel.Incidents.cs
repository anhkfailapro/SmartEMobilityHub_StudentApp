using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>Trang "Sự cố &amp; bảo trì" (UC-OP-03): lọc, chọn sự cố, các nút xử lý và cờ bật/tắt nút.</summary>
public sealed partial class MainViewModel
{
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
            var hub = _demo.Store.FindHub(_selected?.HubId ?? "");
            return hub is not null && hub.Status != HubStatus.Suspended;
        }
    }

    public bool CanResume => _demo.Store.FindHub(_selected?.HubId ?? "")?.Status == HubStatus.Suspended;

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

    public string? ResumeSelectedHub()
    {
        if (_selected is null) return "Hãy chọn một sự cố.";

        var hubId = _selected.HubId;
        if (string.IsNullOrWhiteSpace(hubId)) return "Sự cố không gắn với Hub nào.";

        try
        {
            if (!_demo.Incidents.ResumeHub(hubId, "OP-01")) return $"{hubId} không ở trạng thái tạm dừng.";
            Note($"{hubId} đã mở lại hoạt động.");
        }
        catch (HubNotFoundException ex)
        {
            return ex.Message;
        }

        Refresh();
        return null;
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
