using SmartEMobilityHub.Core.Services;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Models
{
    /// <summary>
    /// Đặt xe của sinh viên: trạng thái, hạn giữ, gia hạn khi quét QR lấy thẳng từ <see cref="CoreDomain.Reservation"/>
    /// của mạng lưới (DC-54). Phần riêng của app (mã nhận xe, số lần quét sai, vị trí đỗ) giữ ở đây.
    /// </summary>
    public sealed class VehicleReservation
    {
        public VehicleReservation(CoreDomain.Reservation core, string slot, string pickUpCode)
        {
            Core = core;
            Slot = slot;
            PickUpCode = pickUpCode;
        }

        public CoreDomain.Reservation Core { get; }

        public string Id => Core.Id;
        public string VehicleCode => Core.VehicleCode;
        public string HubId => Core.HubId;
        public string Slot { get; }
        public DateTime CreatedAt => Core.CreatedAt;
        public DateTime HoldUntil => Core.HoldUntil;
        public string PickUpCode { get; }

        public ReservationState State => Core.Status switch
        {
            CoreDomain.ReservationStatus.PendingPickup => ReservationState.Active,
            CoreDomain.ReservationStatus.PickedUp => ReservationState.Started,
            CoreDomain.ReservationStatus.Completed => ReservationState.Completed,
            CoreDomain.ReservationStatus.Expired => ReservationState.Expired,
            _ => AutoCancelService.IsTimeoutReason(Core.CancelReason) ? ReservationState.Expired : ReservationState.Cancelled
        };

        /// <summary>Lý do hệ thống / người vận hành huỷ (null nếu chưa huỷ).</summary>
        public string? CancelReason => Core.CancelReason;

        /// <summary>Số lần quét sai xe/mã (riêng app Sinh viên).</summary>
        public int WrongAttempts { get; set; }

        public bool IsPickingUp => Core.IsPickingUp;

        /// <summary>Số lần đã gia hạn giữ xe vì đang quét QR (tối đa ServiceConstants.MaxPickupExtensions).</summary>
        public int PickUpExtensions => Core.ExtensionCount;

        public DateTime? ClosedAt => Core.ClosedAt;
    }
}
