namespace SmartEMobilityHub.Models
{
    public class ParkingReservation
    {
        public string Id { get; init; } = "";
        public string PrivateVehicleId { get; init; } = "";
        public string HubId { get; set; } = "";
        public string SlotNumber { get; set; } = "";
        public DateTime Start { get; set; }
        public int DurationMinutes { get; set; }
        public string EntryCode { get; init; } = "";
        public ReservationState State { get; set; }
        public int CancellationFee { get; set; }
        public DateTime? ClosedAt { get; set; }

        public DateTime End => Start.AddMinutes(DurationMinutes);
    }
}
