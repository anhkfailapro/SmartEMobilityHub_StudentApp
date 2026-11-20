namespace SmartEMobilityHub.Models
{
    public class PrivateEVChargingSession
    {
        public string Id { get; init; } = "";
        public string PrivateVehicleId { get; init; } = "";
        public string HubId { get; set; } = "";
        public int PointNumber { get; set; }
        public string SlotNumber { get; set; } = "";
        public DateTime Start { get; set; }
        public int DurationMinutes { get; set; }
        public int TargetBattery { get; set; }
        public SessionState State { get; set; }
        public bool Reminded { get; set; }
        public int CancellationFee { get; set; }
        public DateTime? ClosedAt { get; set; }

        public DateTime End => Start.AddMinutes(DurationMinutes);
    }
}
