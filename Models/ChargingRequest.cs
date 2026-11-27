using SmartEMobilityHub.Services;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Models
{
    /// <summary>
    /// Yêu cầu sạc của sinh viên: điểm ưu tiên, vị trí hàng chờ và cổng được cấp do SmartChargingScheduler của Core
    /// quyết định (DC-54). App chỉ giữ thời điểm gửi và cờ "sinh viên đã huỷ" (Core xoá yêu cầu khi huỷ).
    /// </summary>
    public sealed class ChargingRequest
    {
        private readonly Func<CoreDomain.ChargingRequest, bool> _isLive;
        private readonly Func<string, int> _queuePosition;

        public ChargingRequest(CoreDomain.ChargingRequest core, DateTime createdAt,
            Func<CoreDomain.ChargingRequest, bool> isLive, Func<string, int> queuePosition)
        {
            Core = core;
            CreatedAt = createdAt;
            _isLive = isLive;
            _queuePosition = queuePosition;
        }

        public CoreDomain.ChargingRequest Core { get; }

        public string Id => Core.RequestId;
        public string VehicleCode => Core.VehicleId;
        public string HubId => Core.HubId;
        public double Score => Math.Round(Core.PriorityScore);
        public DateTime CreatedAt { get; }

        /// <summary>Sinh viên đã huỷ yêu cầu này trên app.</summary>
        public bool CancelledByStudent { get; set; }

        public ChargingRequestState State =>
            CancelledByStudent || !_isLive(Core) ? ChargingRequestState.Cancelled
            : Core.AssignedPortId is not null ? ChargingRequestState.Assigned
            : ChargingRequestState.InQueue;

        /// <summary>Vị trí 1, 2, … trong hàng chờ của Hub; 0 khi đã có cổng hoặc đã huỷ.</summary>
        public int QueuePosition => State == ChargingRequestState.InQueue ? _queuePosition(Id) : 0;

        /// <summary>Ước lượng thời gian chờ (hiển thị): mỗi xe phía trước khoảng 5 phút.</summary>
        public int WaitMinutes => QueuePosition * 5;

        public int? AssignedPoint =>
            Core.AssignedPortId is { } port && NetworkMapping.TryParsePointNumber(port, out var number) ? number : null;
    }
}
