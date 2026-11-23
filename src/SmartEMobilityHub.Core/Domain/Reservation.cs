namespace SmartEMobilityHub.Core.Domain;

/// <summary>Đặt chỗ xe — UC-SYS-04 / Bảng 65. isPickingUp quyết định extendHold.</summary>
public sealed class Reservation
{
    public string Id { get; init; } = "";
    public string UserId { get; init; } = "";
    public string HubId { get; init; } = "";
    public string VehicleCode { get; init; } = "";

    public ReservationStatus Status { get; set; } = ReservationStatus.PendingPickup;
    public DateTime CreatedAt { get; init; }
    public DateTime HoldUntil { get; set; }
    public DateTime StartsAt { get; init; }

    /// <summary>Người dùng đang quét QR nhận xe (UC-SYS-04 Exception 5a).</summary>
    public bool IsPickingUp { get; set; }

    /// <summary>Số lần đã gia hạn vì đang quét QR (giả định A10).</summary>
    public int ExtensionCount { get; set; }

    public DateTime? ClosedAt { get; set; }
    public string? CancelReason { get; set; }

    public Reservation Clone() => new()
    {
        Id = Id,
        UserId = UserId,
        HubId = HubId,
        VehicleCode = VehicleCode,
        Status = Status,
        CreatedAt = CreatedAt,
        HoldUntil = HoldUntil,
        StartsAt = StartsAt,
        IsPickingUp = IsPickingUp,
        ExtensionCount = ExtensionCount,
        ClosedAt = ClosedAt,
        CancelReason = CancelReason
    };
}
