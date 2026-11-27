using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// Cổng nhận thao tác từ app Sinh viên vào mạng lưới chung (NetworkStore) mà Trung tâm điều hành đang giám sát.
/// Nhờ đó hai tác nhân dùng chung tài nguyên: sinh viên đặt / nhận / trả xe, báo lỗi, yêu cầu sạc thì
/// Operator thấy ngay; Operator tạm dừng Hub, khoá xe, điều phối thì app Sinh viên đọc lại được.
/// Mọi mã ở đây là mã của Core (HUB-xx, CS-xx-yy); việc đổi mã thuộc về adapter phía app Sinh viên.
/// </summary>
public sealed class StudentAppGateway
{
    public const string CancelledByStudent = "Sinh viên huỷ";
    public const string ExpiredOnStudentApp = "Hết thời gian giữ xe";

    private readonly NetworkStore _store;
    private readonly IncidentManager _incidents;
    private readonly SmartChargingScheduler _charging;
    private readonly IReservationPort _reservations;
    private readonly IClock _clock;
    private readonly AutoCancelService? _autoCancel;

    public StudentAppGateway(
        NetworkStore store,
        IncidentManager incidents,
        SmartChargingScheduler charging,
        IReservationPort reservations,
        IClock clock,
        AutoCancelService? autoCancel = null)
    {
        _store = store;
        _incidents = incidents;
        _charging = charging;
        _reservations = reservations;
        _clock = clock;
        _autoCancel = autoCancel;
    }

    public NetworkStore Store => _store;

    public SmartChargingScheduler Charging => _charging;

    /// <summary>Đưa một xe của app Sinh viên vào mạng lưới nếu Core chưa có (không đổi bộ đếm chỗ đỗ của Hub).</summary>
    public Vehicle EnsureVehicle(string code, VehicleType type, string? hubId, string slot, int battery, DeviceStatus status)
    {
        var existing = _store.FindVehicle(code);
        if (existing is not null) return existing;

        if (hubId is not null) _store.GetHub(hubId);
        var vehicle = new Vehicle
        {
            Code = code,
            Type = type,
            Model = type == VehicleType.EBike ? "Xe đạp điện" : "Xe máy điện",
            HubId = hubId,
            Slot = slot,
            BatteryLevel = Math.Clamp(battery, 0, 100),
            Status = status,
            LastUpdatedAt = _clock.Now
        };
        _store.Vehicles.Add(vehicle);
        return vehicle;
    }

    /// <summary>Lý do Hub không nhận đặt chỗ mới, hoặc null nếu nhận được.</summary>
    public string? GetHubBlockReason(string hubId)
    {
        var hub = _store.FindHub(hubId);
        if (hub is null) return null;
        if (hub.Status == HubStatus.Suspended) return $"{hub.Name} đang tạm dừng hoạt động.";
        return null;
    }

    /// <summary>
    /// Ghi nhận đặt xe. Với đặt xe giữ chỗ, thời điểm phục vụ là hạn nhận xe (StartsAt = HoldUntil),
    /// nên khi Operator tạm dừng Hub, đặt xe đang giữ cũng nằm trong danh sách "sắp tới" bị huỷ.
    /// </summary>
    public Reservation ReserveVehicle(string reservationId, string userId, string vehicleCode, string hubId, DateTime holdUntil)
    {
        var reason = GetHubBlockReason(hubId);
        if (reason is not null) throw new IllegalStateException(reason);

        var vehicle = _store.FindVehicle(vehicleCode)
                      ?? throw new IllegalArgumentException($"Không tìm thấy xe {vehicleCode} trong mạng lưới.");
        if (vehicle.Status != DeviceStatus.Available && vehicle.Status != DeviceStatus.NeedsCharge)
            throw new IllegalStateException($"Xe {vehicleCode} không còn sẵn sàng ({vehicle.Status}).");
        if (_store.Reservations.Any(r => r.Id == reservationId))
            throw new IllegalStateException($"Đặt chỗ {reservationId} đã tồn tại.");

        var now = _clock.Now;
        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = userId,
            HubId = hubId,
            VehicleCode = vehicle.Code,
            Status = ReservationStatus.PendingPickup,
            CreatedAt = now,
            HoldUntil = holdUntil,
            StartsAt = holdUntil
        };
        _store.Reservations.Add(reservation);
        vehicle.Status = DeviceStatus.Reserved;
        vehicle.LastUpdatedAt = now;
        return reservation;
    }

    /// <summary>Sinh viên huỷ hoặc hết hạn giữ xe. Đặt chỗ không còn chờ nhận thì bỏ qua.</summary>
    public bool CloseReservation(string reservationId, string reason)
    {
        var reservation = _store.Reservations.FirstOrDefault(r => r.Id == reservationId);
        if (reservation is null || reservation.Status != ReservationStatus.PendingPickup) return false;

        _reservations.CancelReservation(reservationId, reason);
        return true;
    }

    /// <summary>Sinh viên nhận xe: xe rời Hub, giải phóng một chỗ đỗ.</summary>
    public void PickUp(string reservationId)
    {
        var reservation = _store.Reservations.FirstOrDefault(r => r.Id == reservationId)
                          ?? throw new IllegalArgumentException($"Không tìm thấy đặt chỗ {reservationId}.");
        if (reservation.Status != ReservationStatus.PendingPickup)
            throw new IllegalStateException($"Đặt chỗ {reservationId} không còn chờ nhận xe ({reservation.Status}).");

        _reservations.MarkPickedUp(reservationId);

        var vehicle = _store.FindVehicle(reservation.VehicleCode);
        var hub = _store.FindHub(reservation.HubId);
        if (vehicle is not null)
        {
            vehicle.HubId = null;
            vehicle.LastUpdatedAt = _clock.Now;
        }
        if (hub is not null) hub.OccupiedParkingSlots = Math.Max(0, hub.OccupiedParkingSlots - 1);
    }

    /// <summary>Sinh viên trả xe tại một Hub: chiếm một chỗ đỗ, đóng đặt chỗ đã nhận.</summary>
    public void ReturnVehicle(string vehicleCode, string hubId, string slot, int battery, bool needsCharge)
    {
        var hub = _store.GetHub(hubId);
        var vehicle = _store.FindVehicle(vehicleCode)
                      ?? throw new IllegalArgumentException($"Không tìm thấy xe {vehicleCode} trong mạng lưới.");
        var now = _clock.Now;

        vehicle.HubId = hubId;
        vehicle.Slot = slot;
        vehicle.BatteryLevel = Math.Clamp(battery, 0, 100);
        vehicle.Status = needsCharge ? DeviceStatus.NeedsCharge : DeviceStatus.Available;
        vehicle.AssignedPortId = null;
        vehicle.LastUpdatedAt = now;
        hub.OccupiedParkingSlots = Math.Min(hub.ParkingCapacity, hub.OccupiedParkingSlots + 1);
        hub.LastUpdatedAt = now;

        foreach (var reservation in _store.Reservations.Where(r =>
                     r.VehicleCode == vehicle.Code && r.Status == ReservationStatus.PickedUp))
        {
            reservation.Status = ReservationStatus.Completed;
            reservation.ClosedAt = now;
        }

        // Xe đã được cấp cổng sạc trong lúc đang đi (luồng "trả xe và yêu cầu sạc"):
        // trả đúng Hub có cổng thì xe vào sạc luôn; trả Hub khác thì nhả cổng để xe khác dùng.
        var charging = _store.ChargingRequests.FirstOrDefault(r => r.VehicleId == vehicle.Code && r.AssignedPortId is not null);
        if (charging is not null)
        {
            var point = _store.FindChargingPoint(charging.AssignedPortId!);
            if (point is not null && point.HubId == hubId)
            {
                vehicle.Status = DeviceStatus.Charging;
                vehicle.AssignedPortId = point.Id;
            }
            else
            {
                CancelChargingRequest(charging.RequestId);
            }
        }
    }

    /// <summary>Báo lỗi từ app Sinh viên → hàng đợi sự cố của Operator (gộp báo cáo trùng như UC-OP-03).</summary>
    public IncidentRecord ReportProblem(
        string userId,
        IncidentObjectType objectType,
        string objectCode,
        string problemType,
        string description,
        long photoSizeBytes = 0) =>
        _incidents.ReceiveStudentReport(userId, objectType, objectCode, problemType, description, photoSizeBytes);

    /// <summary>Yêu cầu sạc xe dùng chung → hàng chờ của SmartChargingScheduler, lập lịch ngay cho Hub đó.</summary>
    public ChargingRequest RequestCharging(string requestId, string vehicleCode, string hubId, int battery, DateTime neededAt)
    {
        var hub = _store.GetHub(hubId);
        var existing = _store.ChargingRequests.FirstOrDefault(r => r.RequestId == requestId);
        if (existing is not null) return existing;

        var request = new ChargingRequest
        {
            RequestId = requestId,
            VehicleId = vehicleCode,
            HubId = hubId,
            BatteryLevel = Math.Clamp(battery, 0, 100),
            NeededAt = neededAt,
            QueuePosition = _store.ChargingRequests.Count(r => r.HubId == hubId && r.AssignedPortId is null)
        };
        _store.ChargingRequests.Add(request);
        _charging.RunSchedule(hub);

        // Xe còn đang trong chuyến đi: cổng được giữ cho xe khi tới Hub, nhưng xe vẫn là "đang dùng".
        var vehicle = _store.FindVehicle(vehicleCode);
        if (vehicle is not null && vehicle.HubId is null && vehicle.Status == DeviceStatus.Charging)
            vehicle.Status = DeviceStatus.InUse;
        return request;
    }

    /// <summary>Huỷ yêu cầu sạc; nếu đã được cấp cổng thì trả cổng về trạng thái sẵn sàng.</summary>
    public bool CancelChargingRequest(string requestId)
    {
        var request = _store.ChargingRequests.FirstOrDefault(r => r.RequestId == requestId);
        if (request is null) return false;

        if (request.AssignedPortId is not null)
        {
            var point = _store.FindChargingPoint(request.AssignedPortId);
            if (point is not null && point.Status == DeviceStatus.Charging)
            {
                point.Status = DeviceStatus.Available;
                point.AssignedVehicleCode = null;
            }

            var vehicle = _store.FindVehicle(request.VehicleId);
            if (vehicle is not null && vehicle.AssignedPortId == request.AssignedPortId)
            {
                vehicle.AssignedPortId = null;
                if (vehicle.Status == DeviceStatus.Charging)
                    vehicle.Status = vehicle.HubId is null ? DeviceStatus.InUse : DeviceStatus.Available;
            }
        }

        _store.ChargingRequests.Remove(request);
        return true;
    }

    /// <summary>Đặt / trả một chỗ đỗ cho xe cá nhân: bộ đếm chỗ đỗ của Hub thay đổi để Operator thấy mức sử dụng thật.</summary>
    public void ChangeParkingOccupancy(string hubId, int delta)
    {
        var hub = _store.GetHub(hubId);
        hub.OccupiedParkingSlots = Math.Clamp(hub.OccupiedParkingSlots + delta, 0, hub.ParkingCapacity);
        hub.LastUpdatedAt = _clock.Now;
    }

    // ---------- Bổ sung khi app Sinh viên dùng thẳng mạng lưới (DC-54) ----------

    /// <summary>Sinh viên bắt đầu / thôi quét QR nhận xe (đơn đang quét được gia hạn có giới hạn — A10).</summary>
    public void SetPickingUp(string reservationId, bool pickingUp)
    {
        var reservation = FindReservation(reservationId)
                          ?? throw new IllegalArgumentException($"Không tìm thấy đặt chỗ {reservationId}.");
        if (reservation.Status != ReservationStatus.PendingPickup) return;
        reservation.IsPickingUp = pickingUp;
    }

    /// <summary>Huỷ các đơn quá hạn của một người dùng theo đúng quy tắc UC-SYS-04 (cùng code với chu kỳ quét).</summary>
    public int AutoCancelExpiredFor(string userId) =>
        _autoCancel?.AutoCancelExpiredReservations(userId)
        ?? throw new IllegalStateException("Gateway chưa được nối với AutoCancelService.");

    /// <summary>Không nhận được xác nhận mở khoá: huỷ lệnh, giữ xe RESERVED, tạo sự cố (TCOP03-05).</summary>
    public IncidentRecord ReportUnlockTimeout(string vehicleCode, string reservationId) =>
        _incidents.ReceiveUnlockTimeout(vehicleCode, reservationId);

    /// <summary>Demo: dữ liệu cảm biến của xe đã cũ <paramref name="minutes"/> phút (UC-02 2a).</summary>
    public void MarkVehicleStale(string vehicleCode, int minutes)
    {
        var vehicle = _store.FindVehicle(vehicleCode)
                      ?? throw new IllegalArgumentException($"Không tìm thấy xe {vehicleCode} trong mạng lưới.");
        vehicle.LastUpdatedAt = _clock.Now.AddMinutes(-minutes);
    }

    /// <summary>
    /// Demo UC-08 2a "Tất cả cổng sạc đang bận": các cổng còn trống của Hub được xe khác chiếm.
    /// Trả về số cổng vừa bị chiếm.
    /// </summary>
    public int OccupyFreeChargingPoints(string hubId)
    {
        var hub = _store.GetHub(hubId);
        var free = hub.ChargingPoints.Where(p => p.Status == DeviceStatus.Available).ToList();
        foreach (var point in free)
        {
            point.Status = DeviceStatus.Charging;
            point.AssignedVehicleCode = OtherVehicleCode;
            point.LastUpdatedAt = _clock.Now;
        }
        if (free.Count > 0) hub.LastUpdatedAt = _clock.Now;
        return free.Count;
    }

    /// <summary>Mã giả cho xe khác đang chiếm cổng sạc trong kịch bản demo.</summary>
    public const string OtherVehicleCode = "XE-KHAC";

    /// <summary>
    /// Demo "Cấp cổng sạc cho yêu cầu đầu hàng chờ": nếu Hub không còn cổng trống thì một xe đang sạc
    /// (không phải xe đang chờ) sạc xong và trả cổng; sau đó lập lịch lại. Trả yêu cầu vừa được cấp cổng, hoặc null.
    /// </summary>
    public ChargingRequest? AssignNextChargingPoint(IEnumerable<string> requestIds)
    {
        foreach (var id in requestIds)
        {
            var request = FindChargingRequest(id);
            if (request is null || request.AssignedPortId is not null) continue;

            var hub = _store.GetHub(request.HubId);
            if (!hub.ChargingPoints.Any(p => p.Status == DeviceStatus.Available))
            {
                var waiting = _store.ChargingRequests.Select(r => r.VehicleId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var finishing = hub.ChargingPoints.FirstOrDefault(p =>
                    p.Status == DeviceStatus.Charging && !waiting.Contains(p.AssignedVehicleCode ?? ""));
                if (finishing is null) continue;
                FinishCharging(finishing);
            }

            _charging.RunSchedule(hub);
            if (request.AssignedPortId is not null) return request;
        }
        return null;
    }

    /// <summary>Cổng sạc ngưng hoạt động (UC-09 5a): đánh dấu FAULTY và chuyển xe đang sạc sang cổng khác (UC-SYS-01 5a).</summary>
    public void MarkChargingPointFaulty(string pointId)
    {
        var point = _store.FindChargingPoint(pointId)
                    ?? throw new IllegalArgumentException($"Không tìm thấy cổng sạc {pointId}.");
        _charging.HandleFaultyChargingPoint(point);
    }

    /// <summary>Vị trí (1, 2, …) của yêu cầu trong hàng chờ của Hub; 0 nếu đã có cổng hoặc không còn.</summary>
    public int QueuePositionOf(string requestId)
    {
        var request = FindChargingRequest(requestId);
        if (request is null || request.AssignedPortId is not null) return 0;
        var waiting = _charging.GetQueue(request.HubId).Where(r => r.AssignedPortId is null).ToList();
        return waiting.IndexOf(request) + 1;
    }

    private void FinishCharging(ChargingPoint point)
    {
        var vehicle = point.AssignedVehicleCode is null ? null : _store.FindVehicle(point.AssignedVehicleCode);
        if (vehicle is not null && vehicle.AssignedPortId == point.Id)
        {
            vehicle.AssignedPortId = null;
            vehicle.BatteryLevel = 100;
            if (vehicle.Status == DeviceStatus.Charging) vehicle.Status = DeviceStatus.Available;
            vehicle.LastUpdatedAt = _clock.Now;
        }
        point.Status = DeviceStatus.Available;
        point.AssignedVehicleCode = null;
        point.LastUpdatedAt = _clock.Now;
    }

    public Reservation? FindReservation(string reservationId) =>
        _store.Reservations.FirstOrDefault(r => r.Id == reservationId);

    public Vehicle? FindVehicle(string code) => _store.FindVehicle(code);

    public MobilityHub? FindHub(string hubId) => _store.FindHub(hubId);

    public ChargingRequest? FindChargingRequest(string requestId) =>
        _store.ChargingRequests.FirstOrDefault(r => r.RequestId == requestId);

    public IncidentRecord? FindOpenIncident(string objectCode) => _incidents.FindOpenIncident(objectCode);
}
