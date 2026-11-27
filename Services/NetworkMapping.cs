using SmartEMobilityHub.Models;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Services
{
    /// <summary>
    /// Đổi trạng thái của mạng lưới (Core) sang trạng thái hiển thị của app Sinh viên.
    /// Từ DC-54 app Sinh viên dùng thẳng mã Hub/xe/cổng của Core nên không còn bảng đổi mã Hub.
    /// </summary>
    public static class NetworkMapping
    {
        /// <summary>Trạng thái xe sinh viên thấy. Khoá / đang sửa / hỏng / mất kết nối đều là "Hỏng" (không đặt được).</summary>
        public static VehicleStatus StudentStatus(CoreDomain.DeviceStatus core) => core switch
        {
            CoreDomain.DeviceStatus.Available => VehicleStatus.Available,
            CoreDomain.DeviceStatus.Reserved => VehicleStatus.Reserved,
            CoreDomain.DeviceStatus.InUse => VehicleStatus.InUse,
            CoreDomain.DeviceStatus.NeedsCharge => VehicleStatus.NeedsCharge,
            CoreDomain.DeviceStatus.Charging => VehicleStatus.Charging,
            CoreDomain.DeviceStatus.Relocating => VehicleStatus.Relocating,
            CoreDomain.DeviceStatus.UnderCheck => VehicleStatus.UnderCheck,
            _ => VehicleStatus.Faulty
        };

        /// <summary>Đọc số cổng từ mã cổng của Core: CS-01-03 → 3.</summary>
        public static bool TryParsePointNumber(string portId, out int number)
        {
            number = 0;
            var dash = portId.LastIndexOf('-');
            return dash >= 0 && int.TryParse(portId[(dash + 1)..], out number);
        }
    }
}
