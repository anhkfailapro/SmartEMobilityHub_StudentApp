using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Core.Adapters;

/// <summary>
/// Adapter thông báo trong bộ nhớ (UC-SYS-03/04). Bật <see cref="NetworkDown"/> để
/// giả lập đường truyền lỗi và kích hoạt AlertRetryQueue.
/// </summary>
public sealed class InAppNotificationService : INotificationService
{
    public bool NetworkDown { get; set; }

    /// <summary>Số lần gửi đã thử nhưng thất bại vì <see cref="NetworkDown"/>.</summary>
    public int FailedDeliveries { get; private set; }

    public List<Alert> OperatorAlerts { get; } = new();
    public List<(string UserId, string Message)> UserMessages { get; } = new();
    public List<(string HubId, string Message)> Broadcasts { get; } = new();

    public void PushToOperator(Alert alert)
    {
        if (NetworkDown)
        {
            FailedDeliveries++;
            throw new NotificationDeliveryException("Không gửi được cảnh báo tới màn hình vận hành.");
        }

        OperatorAlerts.Add(alert);
    }

    public void NotifyUser(string userId, string message)
    {
        if (NetworkDown)
        {
            FailedDeliveries++;
            throw new NotificationDeliveryException($"Không gửi được thông báo tới {userId}.");
        }

        UserMessages.Add((userId, message));
    }

    public void BroadcastToHubAffectedUsers(string hubId, string message)
    {
        if (NetworkDown)
        {
            FailedDeliveries++;
            throw new NotificationDeliveryException($"Không gửi được thông báo hàng loạt cho {hubId}.");
        }

        Broadcasts.Add((hubId, message));
    }
}
