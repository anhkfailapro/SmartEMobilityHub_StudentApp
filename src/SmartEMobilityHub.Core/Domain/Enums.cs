namespace SmartEMobilityHub.Core.Domain;

/// <summary>
/// Trạng thái nghiệp vụ của xe, cổng sạc và chỗ đỗ (Sub3 §4.4, Bảng 27).
/// §4.3 gọi là VehicleStatus; nhóm đã dùng tên này trong app nên giữ nguyên,
/// bổ sung Relocating để phục vụ điều phối (xem docs/DESIGN_CHANGES.md DC-03).
/// </summary>
public enum DeviceStatus
{
    Available,
    Reserved,
    InUse,
    Charging,
    NeedsCharge,
    UnderCheck,
    Locked,
    UnderRepair,
    Faulty,
    Offline,
    Relocating
}

/// <summary>Trạng thái vận hành của Hub — tách khỏi DeviceStatus (giả định A5).</summary>
public enum HubStatus
{
    Online,
    Offline,
    Suspended
}

/// <summary>Trạng thái kết nối của Hub/xe/cổng, tách khỏi trạng thái nghiệp vụ (UC-SYS-02).</summary>
public enum ConnectionStatus
{
    Online,
    Offline
}

/// <summary>Vòng đời sự cố (Bảng 27).</summary>
public enum IncidentStatus
{
    New,
    UnderCheck,
    Locked,
    InRepair,
    Resolved,
    Closed
}

/// <summary>Nguồn ghi nhận sự cố (Bảng 27).</summary>
public enum IncidentSource
{
    StudentReport,
    IotSensor,
    UnlockTimeout
}

/// <summary>Mức ưu tiên xử lý sự cố (Bảng 27).</summary>
public enum PriorityLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>Loại cảnh báo vận hành (Bảng 27).</summary>
public enum AlertType
{
    HubOverload,
    StaleData,
    DeviceOffline,
    ChargerFault
}

/// <summary>Trạng thái phiếu bảo trì (Bảng 27).</summary>
public enum MaintenanceStatus
{
    Assigned,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>Trạng thái đặt chỗ xe (UC-SYS-04).</summary>
public enum ReservationStatus
{
    PendingPickup,
    PickedUp,
    Cancelled,
    Expired,
    Completed
}

/// <summary>Loại đối tượng bị báo lỗi (Bảng 42 ghi String; dùng enum cho an toàn kiểu).</summary>
public enum IncidentObjectType
{
    Vehicle,
    ChargingPoint,
    Hub
}

/// <summary>Loại phương tiện.</summary>
public enum VehicleType
{
    EBike,
    EMotorbike
}

/// <summary>Kết quả lệnh cấp nguồn tới cổng sạc (UC-SYS-01 Exception 5a).</summary>
public enum PowerCommandOutcome
{
    Ack,
    Timeout,
    Fault
}
