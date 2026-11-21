using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Ports;

/// <summary>
/// Gửi cảnh báo tới Operator và thông báo tới người dùng (UC-SYS-03, UC-SYS-04).
/// Adapter mặc định: InAppNotificationService (GĐ2) — có thể giả lập mất mạng.
/// </summary>
public interface INotificationService
{
    /// <summary>Ném NotificationDeliveryException khi mạng/timeout để AlertRetryQueue xếp hàng (Exception 4a).</summary>
    void PushToOperator(Alert alert);

    void NotifyUser(string userId, string message);

    /// <summary>Thông báo hàng loạt cho người có reservation bị ảnh hưởng (UC-OP-03 Exception 1a).</summary>
    void BroadcastToHubAffectedUsers(string hubId, string message);
}
