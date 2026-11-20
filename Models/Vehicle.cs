namespace SmartEMobilityHub.Models
{
    public class Vehicle
    {
        public string Code { get; init; } = "";
        public VehicleType Type { get; init; }
        public string? HubId { get; set; }
        public string Slot { get; set; } = "";
        public int Battery { get; set; }
        public VehicleStatus Status { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Online { get; set; } = true;

        public int RangeKm => (int)Math.Round(Battery * (Type == VehicleType.EBike ? 0.46 : 0.78));

        public string TypeLabel => Type == VehicleType.EBike ? "Xe đạp điện" : "Xe máy điện";

        public bool IsStale(DateTime now) => (now - LastUpdate).TotalMinutes > 10;

        public bool IsAvailable => Status == VehicleStatus.Available && Online;
    }
}
