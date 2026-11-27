using SmartEMobilityHub.Data;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Models
{
    /// <summary>
    /// Hub như app Sinh viên hiển thị: đọc thẳng <see cref="CoreDomain.MobilityHub"/> của mạng lưới chung (DC-54).
    /// Chỉ đọc — mọi thay đổi (chỗ đỗ, cổng sạc, tạm dừng) đi qua service Core, nên Operator thấy ngay.
    /// Toạ độ X/Y (km so với vị trí sinh viên) và giới hạn thời gian sạc là dữ liệu hiển thị của app (CampusMap).
    /// </summary>
    public sealed class Hub
    {
        private readonly Func<string, int> _queueLength;
        private List<ChargingPoint>? _points;

        public Hub(CoreDomain.MobilityHub core, Func<string, int> queueLength)
        {
            Core = core;
            _queueLength = queueLength;
            (X, Y) = CampusMap.ToKm(core.Location);
        }

        public CoreDomain.MobilityHub Core { get; }

        public string Id => Core.Id;
        public string Name => Core.Name;
        public string ShortName => Core.ShortName;
        public double X { get; }
        public double Y { get; }
        public int ParkingCapacity => Core.ParkingCapacity;
        public int FreeSlots => Core.FreeParkingSlots;
        public int MaxChargingMinutes => CampusMap.MaxChargingMinutes(Id);

        /// <summary>Số yêu cầu sạc đang chờ cổng tại Hub (hàng chờ của SmartChargingScheduler).</summary>
        public int QueueLength => _queueLength(Id);

        /// <summary>Operator tạm dừng Hub: không nhận đặt xe, đỗ xe, sạc.</summary>
        public bool IsSuspended => Core.Status == CoreDomain.HubStatus.Suspended;

        /// <summary>Mất kết nối cảm biến: dữ liệu có thể không còn đúng.</summary>
        public bool IsOffline => Core.Connection == CoreDomain.ConnectionStatus.Offline;

        public IReadOnlyList<ChargingPoint> Points => _points ??= Core.ChargingPoints.Select(p => new ChargingPoint(p)).ToList();

        public int FreePoints => Points.Count(p => p.Status == PointStatus.Free);

        public double DistanceKmTo(double x, double y) => Math.Sqrt((X - x) * (X - x) + (Y - y) * (Y - y));

        public double DistanceKmTo(Hub other) => DistanceKmTo(other.X, other.Y);
    }
}
