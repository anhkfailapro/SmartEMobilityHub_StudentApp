using SmartEMobilityHub.Services;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Models
{
    /// <summary>
    /// Xe dùng chung như app Sinh viên hiển thị: đọc thẳng <see cref="CoreDomain.Vehicle"/> (DC-54).
    /// Chỉ đọc — đặt, nhận, trả, báo lỗi đều đi qua StudentAppGateway nên Operator thấy cùng một trạng thái.
    /// </summary>
    public sealed class Vehicle
    {
        public Vehicle(CoreDomain.Vehicle core) => Core = core;

        public CoreDomain.Vehicle Core { get; }

        public string Code => Core.Code;
        public VehicleType Type => Core.Type;
        public string Model => Core.Model;
        public string? HubId => Core.HubId;
        public string Slot => Core.Slot;
        public int Battery => Core.BatteryLevel;
        public VehicleStatus Status => NetworkMapping.StudentStatus(Core.Status);
        public DateTime LastUpdate => Core.LastUpdatedAt;
        public bool Online => Core.Connection == CoreDomain.ConnectionStatus.Online;

        public int RangeKm => (int)Math.Round(Battery * (Type == VehicleType.EBike ? 0.46 : 0.78));
        public string TypeLabel => Type == VehicleType.EBike ? "Xe đạp điện" : "Xe máy điện";
        public bool IsStale(DateTime now) => (now - LastUpdate).TotalMinutes > 10;
        public bool IsAvailable => Status == VehicleStatus.Available && Online;
    }
}
