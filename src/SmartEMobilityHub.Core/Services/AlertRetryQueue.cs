using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// Hàng đợi gửi lại cảnh báo (Sub3 Bảng 32/33). Lỗi mạng không làm mất cảnh báo:
/// alert được giữ lại và gửi lại sau 30 giây, tối đa 5 lần (giả định A1).
/// </summary>
public sealed class AlertRetryQueue : IAlertSink
{
    public const int RetryDelayMs = 30_000;
    public const int MaxRetry = 5;

    private readonly INotificationService _notification;
    private readonly IClock _clock;
    private readonly ISystemLogger _logger;
    private readonly List<Alert> _pending = new();
    private readonly Dictionary<string, DateTime> _nextRetryAt = new();

    public AlertRetryQueue(INotificationService notification, IClock clock, ISystemLogger logger)
    {
        _notification = notification;
        _clock = clock;
        _logger = logger;
    }

    public IReadOnlyList<Alert> Pending => _pending;

    public int Size() => _pending.Count;

    /// <summary>Gửi ngay; thất bại thì đưa vào hàng đợi và trả false.</summary>
    public bool DispatchAlert(Alert alert)
    {
        try
        {
            _notification.PushToOperator(alert);
            alert.MarkDelivered();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Gửi cảnh báo {alert.AlertId} thất bại: {ex.Message}", "UC-SYS-03");
            EnqueueAlert(alert);
            return false;
        }
    }

    public void EnqueueAlert(Alert alert)
    {
        if (_pending.Any(a => a.AlertId == alert.AlertId)) return;

        if (alert.RetryCount >= MaxRetry)
        {
            _logger.Error($"Cảnh báo {alert.AlertId} vượt {MaxRetry} lần gửi lại — loại bỏ.", "UC-SYS-03");
            return;
        }

        _pending.Add(alert);
        _nextRetryAt[alert.AlertId] = _clock.Now.AddMilliseconds(RetryDelayMs);
    }

    /// <summary>Gửi lại các alert đến hạn; trả về số alert gửi thành công.</summary>
    public int ProcessQueue(DateTime now)
    {
        var sent = 0;

        foreach (var alert in _pending.ToList())
        {
            if (!_nextRetryAt.TryGetValue(alert.AlertId, out var dueAt) || dueAt > now) continue;

            if (RetryPushAlert(alert))
            {
                _pending.Remove(alert);
                _nextRetryAt.Remove(alert.AlertId);
                sent++;
                continue;
            }

            alert.IncreaseRetry();
            if (alert.RetryCount >= MaxRetry)
            {
                _pending.Remove(alert);
                _nextRetryAt.Remove(alert.AlertId);
                _logger.Error($"Cảnh báo {alert.AlertId} thất bại {MaxRetry} lần — loại bỏ.", "UC-SYS-03");
            }
            else
            {
                _nextRetryAt[alert.AlertId] = now.AddMilliseconds(RetryDelayMs);
            }
        }

        return sent;
    }

    private bool RetryPushAlert(Alert alert)
    {
        try
        {
            _notification.PushToOperator(alert);
            alert.MarkDelivered();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
