using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// UC-OP-03 — xử lý sự cố vận hành (Sub3 Bảng 40/41). Tiếp nhận báo cáo sinh viên,
/// cảnh báo cảm biến, lỗi mở khoá; khoá thiết bị; tạo phiếu bảo trì; gán kỹ thuật viên;
/// tạm dừng Hub (UC-OP-03 Exception 1a).
/// </summary>
public sealed class IncidentManager
{
    private static readonly Dictionary<IncidentStatus, IncidentStatus[]> AllowedTransitions = new()
    {
        [IncidentStatus.New] = new[] { IncidentStatus.UnderCheck, IncidentStatus.Locked },
        [IncidentStatus.UnderCheck] = new[] { IncidentStatus.Locked, IncidentStatus.InRepair },
        [IncidentStatus.Locked] = new[] { IncidentStatus.InRepair },
        [IncidentStatus.InRepair] = new[] { IncidentStatus.Resolved },
        [IncidentStatus.Resolved] = new[] { IncidentStatus.Closed },
        [IncidentStatus.Closed] = Array.Empty<IncidentStatus>()
    };

    private readonly NetworkStore _store;
    private readonly INotificationService _notification;
    private readonly IReservationPort _reservations;
    private readonly IClock _clock;
    private readonly ISystemLogger _logger;

    public IncidentManager(
        NetworkStore store,
        INotificationService notification,
        IReservationPort reservations,
        IClock clock,
        ISystemLogger logger)
    {
        _store = store;
        _notification = notification;
        _reservations = reservations;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Bảng 41 — báo lỗi từ sinh viên; ảnh &gt; 5 MB bị từ chối.</summary>
    public IncidentRecord ReceiveStudentReport(
        string userId,
        IncidentObjectType objectType,
        string objectCode,
        string problemType,
        string description,
        long photoSizeBytes = 0)
    {
        if (photoSizeBytes > ServiceConstants.MaxPhotoBytes)
        {
            var megabytes = photoSizeBytes / 1024.0 / 1024.0;
            throw new FileTooLargeException(
                $"Ảnh {megabytes:0.0} MB vượt giới hạn 5 MB. Vui lòng chọn ảnh nhỏ hơn.");
        }

        if (string.IsNullOrWhiteSpace(objectCode))
            throw new IllegalArgumentException("Mã đối tượng không được để trống.");

        objectCode = objectCode.Trim();
        EnsureObjectExists(objectType, objectCode);

        var open = FindOpenIncident(objectCode);
        if (open is not null)
        {
            open.AddReport(userId, description);
            AddLog(open, $"Gộp báo cáo trùng từ {userId}: {description}", "Sinh viên");
            NotifyUserSafely(userId, $"Sự cố {open.IncidentId} đã được ghi nhận trước đó. Báo cáo của bạn đã được gộp.");
            return open;
        }

        var incident = new IncidentRecord
        {
            IncidentId = _store.NextIncidentId(),
            ObjectType = objectType,
            ObjectCode = objectCode,
            HubId = ResolveHubId(objectType, objectCode),
            Source = IncidentSource.StudentReport,
            ProblemType = problemType,
            Description = description,
            CreatedAt = _clock.Now,
            Status = IncidentStatus.New,
            Priority = PriorityLevel.Medium
        };

        incident.ReporterIds.Add(userId);
        AddLog(incident, $"Ghi nhận báo cáo: {problemType}.", "Sinh viên");
        _store.Incidents.Add(incident);

        SetDeviceStatus(incident, DeviceStatus.UnderCheck);
        NotifyUserSafely(userId, $"Đã tiếp nhận báo cáo {incident.IncidentId} cho {objectCode}. Người vận hành đang xử lý.");

        return incident;
    }

    /// <summary>Bảng 41 — cảnh báo từ cảm biến; HUB_OVERLOAD là CRITICAL (giả định A7).</summary>
    public IncidentRecord ReceiveSensorAlert(string hubId, string objectCode, AlertType alertType)
    {
        var objectType = ResolveObjectType(objectCode);
        var priority = alertType switch
        {
            AlertType.HubOverload => PriorityLevel.Critical,
            AlertType.DeviceOffline => PriorityLevel.High,
            AlertType.ChargerFault => PriorityLevel.High,
            AlertType.PowerLimit => PriorityLevel.High,
            _ => PriorityLevel.Medium
        };

        var open = FindOpenIncident(objectCode);
        if (open is not null)
        {
            if (priority > open.Priority) open.Priority = priority;
            AddLog(open, $"Cảnh báo bổ sung từ cảm biến: {alertType}.", "Cảm biến IoT");
            return open;
        }

        var incident = new IncidentRecord
        {
            IncidentId = _store.NextIncidentId(),
            ObjectType = objectType,
            ObjectCode = objectCode,
            HubId = hubId,
            Source = IncidentSource.IotSensor,
            ProblemType = $"Cảnh báo {alertType}",
            Description = BuildSensorDescription(hubId, objectCode, alertType),
            CreatedAt = _clock.Now,
            Status = IncidentStatus.New,
            Priority = priority
        };

        AddLog(incident, $"Ghi nhận cảnh báo {alertType} từ cảm biến.", "Cảm biến IoT");
        _store.Incidents.Add(incident);
        return incident;
    }

    /// <summary>Bảng 41 + UC-S-06: không xác nhận mở khoá sau 30 giây.</summary>
    public IncidentRecord ReceiveUnlockTimeout(string vehicleId, string reservationId)
    {
        var reservation = _store.Reservations.FirstOrDefault(r => r.Id == reservationId);
        if (reservation is not null)
            _reservations.CancelReservation(reservation.Id, $"Không xác nhận mở khoá sau {ServiceConstants.UnlockTimeoutSeconds} giây.");

        var vehicle = _store.FindVehicle(vehicleId);
        if (vehicle is not null && vehicle.Status != DeviceStatus.InUse)
            vehicle.Status = DeviceStatus.Reserved;

        var incident = new IncidentRecord
        {
            IncidentId = _store.NextIncidentId(),
            ObjectType = IncidentObjectType.Vehicle,
            ObjectCode = vehicleId,
            HubId = vehicle?.HubId ?? reservation?.HubId ?? "",
            Source = IncidentSource.UnlockTimeout,
            ProblemType = "Lỗi mở khoá",
            Description = $"Không nhận được xác nhận mở khoá sau {ServiceConstants.UnlockTimeoutSeconds} giây. " +
                          $"Vui lòng liên hệ {ServiceConstants.SupportHotline}.",
            CreatedAt = _clock.Now,
            Status = IncidentStatus.New,
            Priority = PriorityLevel.High
        };

        AddLog(incident, "Không nhận ACK mở khoá — đã huỷ lệnh và giữ xe ở trạng thái RESERVED.", "Hệ thống");
        _store.Incidents.Add(incident);

        if (reservation is not null)
            NotifyUserSafely(reservation.UserId, $"Không mở được khoá xe {vehicleId}. Liên hệ {ServiceConstants.SupportHotline}.");

        return incident;
    }

    public IncidentRecord? FindOpenIncident(string objectCode) =>
        _store.Incidents.FirstOrDefault(i =>
            string.Equals(i.ObjectCode, objectCode, StringComparison.OrdinalIgnoreCase) && i.IsOpen());

    /// <summary>Bảng 41 — tạm khoá thiết bị; đã khoá thì trả true (idempotent).</summary>
    public bool LockDevice(string incidentId)
    {
        var incident = _store.GetIncident(incidentId);
        if (!incident.IsOpen())
            throw new IllegalStateException($"Sự cố {incidentId} đã đóng, không thể khoá thiết bị.");

        if (ReadDeviceStatus(incident) == DeviceStatus.Locked) return true;

        SetDeviceStatus(incident, DeviceStatus.Locked);
        if (incident.Status is IncidentStatus.New or IncidentStatus.UnderCheck)
            Transition(incident, IncidentStatus.Locked);

        AddLog(incident, $"Tạm khoá thiết bị {incident.ObjectCode}.", "Người vận hành");
        return true;
    }

    /// <summary>Bảng 41 — tạo phiếu bảo trì; sự cố CLOSED thì không được.</summary>
    public MaintenanceTask CreateMaintenanceTask(string incidentId)
    {
        var incident = _store.GetIncident(incidentId);
        if (incident.Status == IncidentStatus.Closed)
            throw new IllegalStateException($"Sự cố {incidentId} đã đóng, không thể tạo phiếu.");

        var task = new MaintenanceTask
        {
            TaskId = _store.NextTaskId(),
            IncidentId = incident.IncidentId,
            Status = MaintenanceStatus.Assigned,
            CreatedAt = _clock.Now,
            SlaDeadline = _clock.Now + ServiceConstants.SlaFor(incident.Priority)
        };

        _store.MaintenanceTasks.Add(task);
        AddLog(incident, $"Tạo phiếu bảo trì {task.TaskId} (SLA {task.SlaDeadline:HH:mm}).", "Người vận hành");
        return task;
    }

    /// <summary>Bảng 41 — gán kỹ thuật viên đang sẵn sàng; bận/không tồn tại trả false.</summary>
    public bool AssignTechnician(string taskId, string techId)
    {
        var task = _store.MaintenanceTasks.FirstOrDefault(t => t.TaskId == taskId);
        if (task is null) return false;

        var technician = _store.Technicians.FirstOrDefault(t => t.TechId == techId);
        if (technician is null || !technician.IsAvailable()) return false;

        var incident = _store.FindIncident(task.IncidentId);
        if (incident is not null && !incident.IsOpen()) return false;

        // Gán lại cho người khác thì trả người cũ về trạng thái sẵn sàng.
        if (task.AssignedTech is not null && task.AssignedTech.TechId != technician.TechId)
            task.AssignedTech.Available = true;

        task.Assign(technician, _clock.Now, ServiceConstants.SlaFor(incident?.Priority ?? PriorityLevel.Medium));
        technician.Available = false;

        if (incident is not null)
        {
            if (incident.Status == IncidentStatus.New) Transition(incident, IncidentStatus.UnderCheck);
            if (incident.Status is IncidentStatus.UnderCheck or IncidentStatus.Locked)
                Transition(incident, IncidentStatus.InRepair);

            SetDeviceStatus(incident, DeviceStatus.UnderRepair);
            AddLog(incident, $"Gán {technician.Name} ({technician.Team}) xử lý.", "Người vận hành");
        }

        return true;
    }

    /// <summary>Bảng 41/47 — kỹ thuật viên sẵn sàng gần Hub nhất; không có thì null.</summary>
    public Technician? SuggestTechnician(string hubId)
    {
        var hub = _store.FindHub(hubId);
        if (hub is null) return null;

        return _store.Technicians
            .Where(t => t.IsAvailable())
            .OrderBy(t => t.DistanceToHub(hub))
            .ThenBy(t => t.TechId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>Bảng 41 — tạm dừng Hub, huỷ reservation sắp tới, trả về số đơn đã huỷ.</summary>
    public int SuspendHub(string hubId, string operatorId)
    {
        var hub = _store.GetHub(hubId);
        hub.Status = HubStatus.Suspended;

        var cancelled = 0;
        var upcoming = _reservations.FindUpcomingByHub(hubId, _clock.Now);

        foreach (var reservation in upcoming)
        {
            try
            {
                _reservations.CancelReservation(reservation.Id, "Hub tạm dừng hoạt động");
                cancelled++;
            }
            catch (Exception ex)
            {
                _logger.Error($"Không huỷ được {reservation.Id}: {ex.Message}", "UC-OP-03");
            }
        }

        _logger.Warn($"{operatorId} tạm dừng {hub.ShortName} — huỷ {cancelled} đặt chỗ.", "UC-OP-03");

        try
        {
            _notification.BroadcastToHubAffectedUsers(
                hubId,
                $"{hub.ShortName} tạm dừng hoạt động. {cancelled} đặt chỗ sắp tới đã được huỷ và hoàn tài nguyên.");
        }
        catch (NotificationDeliveryException ex)
        {
            _logger.Warn($"Đã tạm dừng {hubId} nhưng không gửi được thông báo hàng loạt: {ex.Message}", "UC-OP-03");
        }

        return cancelled;
    }

    /// <summary>
    /// Bảng 41 — mở lại Hub đã tạm dừng. Trả false nếu Hub không ở trạng thái SUSPENDED.
    /// Hub đang mất kết nối thì mở lại về OFFLINE, chu kỳ đồng bộ sẽ đưa lên ONLINE khi có tín hiệu.
    /// </summary>
    public bool ResumeHub(string hubId, string operatorId)
    {
        var hub = _store.GetHub(hubId);
        if (hub.Status != HubStatus.Suspended) return false;

        hub.Status = hub.Connection == ConnectionStatus.Online ? HubStatus.Online : HubStatus.Offline;
        _logger.Info($"{operatorId} mở lại {hub.ShortName}.", "UC-OP-03");
        return true;
    }

    /// <summary>
    /// Kết thúc sửa chữa: IN_REPAIR → RESOLVED, hoàn thành các phiếu đang mở, trả kỹ thuật viên
    /// về sẵn sàng và đưa thiết bị về AVAILABLE để sinh viên dùng lại.
    /// </summary>
    public void ResolveIncident(string incidentId, string note)
    {
        var incident = _store.GetIncident(incidentId);
        Transition(incident, IncidentStatus.Resolved);

        var openTasks = _store.MaintenanceTasks.Where(t =>
            t.IncidentId == incident.IncidentId &&
            t.Status is MaintenanceStatus.Assigned or MaintenanceStatus.InProgress);

        foreach (var task in openTasks)
        {
            if (task.Status == MaintenanceStatus.Assigned) task.Start();
            task.Complete(note, _clock.Now);
            if (task.AssignedTech is not null) task.AssignedTech.Available = true;
        }

        SetDeviceStatus(incident, DeviceStatus.Available);
        AddLog(incident, $"Hoàn tất sửa chữa: {note}", "Người vận hành");
    }

    /// <summary>
    /// Bảng 41 — chỉ cho phép chuyển trạng thái hợp lệ (giải thích C6).
    /// Chuyển sang RESOLVED đi qua <see cref="ResolveIncident"/> để thiết bị và phiếu được giải phóng.
    /// </summary>
    public void UpdateIncidentStatus(string incidentId, IncidentStatus status)
    {
        if (status == IncidentStatus.Resolved)
        {
            ResolveIncident(incidentId, "Đã xử lý xong.");
            return;
        }

        var incident = _store.GetIncident(incidentId);
        Transition(incident, status);
    }

    /// <summary>Bảng 41 — lọc theo Hub/trạng thái, sắp theo ưu tiên rồi thời gian.</summary>
    public List<IncidentRecord> GetIncidentQueue(string? hubId = null, IncidentStatus? status = null)
    {
        return _store.Incidents
            .Where(i => hubId is null || i.HubId == hubId)
            .Where(i => status is null || i.Status == status)
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.CreatedAt)
            .ToList();
    }

    private void Transition(IncidentRecord incident, IncidentStatus next)
    {
        if (incident.Status == next) return;

        if (!AllowedTransitions.TryGetValue(incident.Status, out var allowed) || !allowed.Contains(next))
            throw new IllegalStateException($"Không thể chuyển sự cố {incident.IncidentId} từ {incident.Status} sang {next}.");

        incident.Status = next;
        AddLog(incident, $"Chuyển trạng thái sang {next}.", "Người vận hành");
    }

    private void NotifyUserSafely(string userId, string message)
    {
        try
        {
            _notification.NotifyUser(userId, message);
        }
        catch (NotificationDeliveryException ex)
        {
            _logger.Warn($"Không gửi được thông báo tới {userId}: {ex.Message}", "IncidentManager");
        }
    }

    private void EnsureObjectExists(IncidentObjectType objectType, string objectCode)
    {
        var exists = objectType switch
        {
            IncidentObjectType.Vehicle => _store.FindVehicle(objectCode) is not null,
            IncidentObjectType.ChargingPoint => _store.FindChargingPoint(objectCode) is not null,
            _ => _store.FindHub(objectCode) is not null
        };

        if (!exists) throw new IllegalArgumentException($"Không tìm thấy {objectCode} trong mạng lưới.");
    }

    private string ResolveHubId(IncidentObjectType objectType, string objectCode)
    {
        return objectType switch
        {
            IncidentObjectType.Vehicle => _store.FindVehicle(objectCode)?.HubId ?? "",
            IncidentObjectType.ChargingPoint => _store.FindChargingPoint(objectCode)?.HubId ?? "",
            _ => objectCode
        };
    }

    private static IncidentObjectType ResolveObjectType(string objectCode)
    {
        if (objectCode.StartsWith("CS-", StringComparison.OrdinalIgnoreCase)) return IncidentObjectType.ChargingPoint;
        if (objectCode.StartsWith("HUB-", StringComparison.OrdinalIgnoreCase)) return IncidentObjectType.Hub;
        return IncidentObjectType.Vehicle;
    }

    private string BuildSensorDescription(string hubId, string objectCode, AlertType alertType)
    {
        var hub = _store.FindHub(hubId);
        return alertType switch
        {
            AlertType.HubOverload =>
                $"{hub?.ShortName ?? hubId} đạt {hub?.MaxUtilization * 100:0}% sức chứa ({hub?.OccupiedParkingSlots}/{hub?.ParkingCapacity} chỗ).",
            AlertType.StaleData => $"Dữ liệu {objectCode} đã cũ hơn {ServiceConstants.StaleThresholdMinutes} phút.",
            AlertType.DeviceOffline => $"{objectCode} mất kết nối cảm biến.",
            AlertType.PowerLimit => $"{hub?.ShortName ?? hubId} có kế hoạch sạc vượt giới hạn {hub?.PowerLimitKw:0.#} kW.",
            _ => $"{objectCode} báo lỗi cổng sạc."
        };
    }

    private DeviceStatus? ReadDeviceStatus(IncidentRecord incident)
    {
        return incident.ObjectType switch
        {
            IncidentObjectType.Vehicle => _store.FindVehicle(incident.ObjectCode)?.Status,
            IncidentObjectType.ChargingPoint => _store.FindChargingPoint(incident.ObjectCode)?.Status,
            _ => null
        };
    }

    private void SetDeviceStatus(IncidentRecord incident, DeviceStatus status)
    {
        switch (incident.ObjectType)
        {
            case IncidentObjectType.Vehicle:
                var vehicle = _store.FindVehicle(incident.ObjectCode);
                if (vehicle is not null && vehicle.Status != DeviceStatus.InUse) vehicle.Status = status;
                break;
            case IncidentObjectType.ChargingPoint:
                var point = _store.FindChargingPoint(incident.ObjectCode);
                if (point is not null) point.Status = status;
                break;
        }
    }

    private void AddLog(IncidentRecord incident, string message, string actor)
    {
        incident.Logs.Add(new IncidentLogEntry { At = _clock.Now, Message = message, Actor = actor });
    }
}
