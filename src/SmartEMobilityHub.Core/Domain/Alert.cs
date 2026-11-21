namespace SmartEMobilityHub.Core.Domain;

/// <summary>Cảnh báo vận hành (Bảng 34) — đơn vị của AlertRetryQueue.</summary>
public sealed class Alert
{
    public string AlertId { get; init; } = "";
    public AlertType Type { get; init; }
    public string HubId { get; init; } = "";
    public string Message { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public int RetryCount { get; private set; }
    public bool Delivered { get; private set; }

    /// <summary>Bảng 35: đánh dấu đã gửi thành công tới Operator.</summary>
    public void MarkDelivered() => Delivered = true;

    /// <summary>Bảng 35: tăng số lần thử, trả về giá trị mới.</summary>
    public int IncreaseRetry() => ++RetryCount;

    public Alert Clone() => new()
    {
        AlertId = AlertId,
        Type = Type,
        HubId = HubId,
        Message = Message,
        CreatedAt = CreatedAt,
        RetryCount = RetryCount,
        Delivered = Delivered
    };
}
