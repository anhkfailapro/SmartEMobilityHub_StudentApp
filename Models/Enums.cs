namespace SmartEMobilityHub.Models
{
    public enum VehicleStatus { Available, Reserved, InUse, NeedsCharge, Charging, Faulty, Relocating, UnderCheck }

    public enum VehicleType { EBike, EMotorbike }

    public enum ReservationState { Active, Started, Cancelled, Expired, Completed }

    public enum ReservationType { Vehicle, Parking, Charging }

    public enum SessionState { Pending, Charging, Completed, Failed, Cancelled }

    public enum ChargingRequestState { InQueue, Assigned, Cancelled }

    public enum PointStatus { Free, Charging, OutOfService }

    public enum PriorityLevel { Low, Medium, High, Critical }

    public enum IncidentStatus { New, UnderCheck, Locked, InRepair, Resolved, Closed }

    public enum IncidentSource { StudentReport, IotSensor, UnlockTimeout }

    public enum ReportObjectKind { Vehicle, ChargingPoint }

    public enum BookingKind { Vehicle, Parking, ChargingSession, ChargingRequest }

    public enum Tone { Info, Success, Warning, Danger, Charging, Neutral }
}
