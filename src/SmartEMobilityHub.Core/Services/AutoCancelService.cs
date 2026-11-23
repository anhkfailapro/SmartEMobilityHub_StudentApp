using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// UC-SYS-04 — tự huỷ đặt chỗ quá hạn (Sub3 Bảng 29, nhánh #autoCancelExpiredReservations).
/// Lỗi ở một đơn chỉ ghi log, không dừng vòng lặp (NFR 2.2.2).
/// </summary>
public sealed class AutoCancelService
{
    private readonly IReservationPort _reservations;
    private readonly INotificationService _notification;
    private readonly IClock _clock;
    private readonly ISystemLogger _logger;

    public AutoCancelService(
        IReservationPort reservations,
        INotificationService notification,
        IClock clock,
        ISystemLogger logger)
    {
        _reservations = reservations;
        _notification = notification;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Trả về số đơn đã huỷ (Bảng 29 trả int).</summary>
    public int AutoCancelExpiredReservations()
    {
        var now = _clock.Now;
        var cancelled = 0;

        var expired = _reservations.FindExpiredReservations(now, ServiceConstants.ReservationHoldMinutes);

        foreach (var reservation in expired)
        {
            try
            {
                if (reservation.IsPickingUp && reservation.ExtensionCount < ServiceConstants.MaxPickupExtensions)
                {
                    // Exception 5a: người dùng đang quét QR — gia hạn thay vì huỷ.
                    _reservations.ExtendHold(reservation.Id, 1);
                    _logger.Info($"Gia hạn {reservation.Id} vì người dùng đang nhận xe.", "UC-SYS-04");
                    continue;
                }

                var reason = reservation.IsPickingUp
                    ? $"Quét QR quá {ServiceConstants.MaxPickupExtensions} lần gia hạn mà chưa nhận được xe"
                    : "Quá 15 phút giữ chỗ";
                _reservations.CancelReservation(reservation.Id, reason);
            }
            catch (Exception ex)
            {
                _logger.Error($"Không huỷ được {reservation.Id}: {ex.Message}", "UC-SYS-04");
                continue;
            }

            cancelled++;
            _logger.Info($"Đã huỷ {reservation.Id} quá hạn giữ chỗ.", "UC-SYS-04");

            // Mất mạng thông báo không được làm sai kết quả huỷ đơn.
            try
            {
                _notification.NotifyUser(
                    reservation.UserId,
                    $"Đặt chỗ {reservation.Id} tại {reservation.HubId} đã bị huỷ sau {ServiceConstants.ReservationHoldMinutes} phút giữ chỗ. Xe {reservation.VehicleCode} đã trở lại trạng thái sẵn sàng.");
            }
            catch (NotificationDeliveryException ex)
            {
                _logger.Warn($"Đã huỷ {reservation.Id} nhưng không gửi được thông báo: {ex.Message}", "UC-SYS-04");
            }
        }

        return cancelled;
    }
}
