namespace SmartEMobilityHub.Models
{
    public class Hub
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string ShortName { get; init; } = "";
        public double X { get; init; }
        public double Y { get; init; }
        public int ParkingCapacity { get; init; }
        public int FreeSlots { get; set; }
        public int MaxChargingMinutes { get; init; }
        public int QueueLength { get; set; }
        /// <summary>Operator tạm dừng Hub (đồng bộ từ Trung tâm điều hành): không nhận đặt xe, đỗ xe, sạc.</summary>
        public bool IsSuspended { get; set; }
        public List<ChargingPoint> Points { get; } = new();

        public int FreePoints => Points.Count(p => p.Status == PointStatus.Free);

        public double DistanceKmTo(double x, double y) => Math.Sqrt((X - x) * (X - x) + (Y - y) * (Y - y));

        public double DistanceKmTo(Hub other) => DistanceKmTo(other.X, other.Y);
    }
}
