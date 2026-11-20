namespace SmartEMobilityHub.Models
{
    public class VehicleReservation
    {
        public string Id { get; init; } = "";
        public string VehicleCode { get; init; } = "";
        public string HubId { get; init; } = "";
        public string Slot { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public DateTime HoldUntil { get; set; }
        public string PickUpCode { get; init; } = "";
        public ReservationState State { get; set; }
        public int WrongAttempts { get; set; }
        public bool IsPickingUp { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
