namespace SmartEMobilityHub.Models
{
    public class ChargingRequest
    {
        public string Id { get; init; } = "";
        public string VehicleCode { get; init; } = "";
        public string HubId { get; init; } = "";
        public double Score { get; init; }
        public int QueuePosition { get; set; }
        public int WaitMinutes { get; set; }
        public ChargingRequestState State { get; set; }
        public int? AssignedPoint { get; set; }
        public DateTime CreatedAt { get; init; }
    }
}
