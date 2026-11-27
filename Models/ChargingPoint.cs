using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Models
{
    /// <summary>Cổng sạc như sinh viên thấy: Trống / Đang sạc / Ngưng hoạt động, đọc từ cổng của Core (DC-54).</summary>
    public sealed class ChargingPoint
    {
        public ChargingPoint(CoreDomain.ChargingPoint core) => Core = core;

        public CoreDomain.ChargingPoint Core { get; }

        /// <summary>Mã cổng của mạng lưới, ví dụ CS-01-03.</summary>
        public string Id => Core.Id;

        public int Number => Core.Number;

        public PointStatus Status => Core.Status switch
        {
            CoreDomain.DeviceStatus.Available => PointStatus.Free,
            CoreDomain.DeviceStatus.Charging => PointStatus.Charging,
            _ => PointStatus.OutOfService
        };
    }
}
