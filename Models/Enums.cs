namespace SmartEMobilityHub.Models
{
    // Các enum trùng với Core (VehicleType, PriorityLevel, IncidentStatus, IncidentSource) đã bỏ:
    // app Sinh viên dùng thẳng enum của Core qua CoreAliases.cs (DC-54).
    // Các enum dưới đây là trạng thái hiển thị riêng của app Sinh viên.

    /// <summary>Trạng thái xe như sinh viên thấy (Core có nhiều trạng thái hơn, xem NetworkMapping.StudentStatus).</summary>
    public enum VehicleStatus { Available, Reserved, InUse, NeedsCharge, Charging, Faulty, Relocating, UnderCheck }

    public enum ReservationState { Active, Started, Cancelled, Expired, Completed }

    public enum ReservationType { Vehicle, Parking, Charging }

    public enum SessionState { Pending, Charging, Completed, Failed, Cancelled }

    public enum ChargingRequestState { InQueue, Assigned, Cancelled }

    public enum PointStatus { Free, Charging, OutOfService }

    public enum ReportObjectKind { Vehicle, ChargingPoint }

    public enum BookingKind { Vehicle, Parking, ChargingSession, ChargingRequest }

    public enum Tone { Info, Success, Warning, Danger, Charging, Neutral }
}
