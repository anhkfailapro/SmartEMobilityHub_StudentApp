namespace SmartEMobilityHub.Models
{
    public class Trip
    {
        public string Id { get; init; } = "";
        public string VehicleCode { get; init; } = "";
        public string StartHubId { get; init; } = "";
        public DateTime StartTime { get; init; }
        public int StartBattery { get; init; }
        public DateTime? EndTime { get; set; }
        public string? EndHubId { get; set; }
        public string EndSlot { get; set; } = "";
        public double DistanceKm { get; set; }
        public int ExtraFreeMinutes { get; set; }
        public Invoice? Invoice { get; set; }
    }
}
