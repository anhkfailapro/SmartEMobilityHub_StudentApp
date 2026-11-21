using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Ports;

/// <summary>
/// Cầu nối tới module Reservation (Trường). Mặc định dùng InMemoryReservationAdapter;
/// khi ReservationManager của Trường sẵn sàng thì viết adapter khác, không sửa AutoCancelService.
/// </summary>
public interface IReservationPort
{
    /// <summary>
    /// Reservation PENDING_PICKUP đã quá hạn giữ chỗ. Ngưỡng lấy theo tham số holdMinutes
    /// (demo/test có thể truyền 15) — UC-SYS-04 Bảng 65.
    /// </summary>
    IReadOnlyList<Reservation> FindExpiredReservations(DateTime now, int holdMinutes);

    /// <summary>Reservation sắp tới tại Hub (UC-OP-03 Exception 1a).</summary>
    IReadOnlyList<Reservation> FindUpcomingByHub(string hubId, DateTime now);

    void CancelReservation(string reservationId, string reason);

    /// <summary>Gia hạn giữ chỗ khi người dùng đang quét QR (Exception 5a).</summary>
    void ExtendHold(string reservationId, int extraMinutes);

    /// <summary>Đánh dấu đã nhận xe — chu kỳ sau không huỷ nữa.</summary>
    void MarkPickedUp(string reservationId);
}
