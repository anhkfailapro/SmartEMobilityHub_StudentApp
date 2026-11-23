namespace SmartEMobilityHub.Core.Domain;

public enum DispatchOrderStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public sealed class DispatchOrder
{
    public string OrderId { get; init; } = "";
    public string SourceHubId { get; init; } = "";
    public string TargetHubId { get; init; } = "";
    public List<string> VehicleIds { get; init; } = new();
    public string StaffId { get; init; } = "";
    public string Reason { get; set; } = "";
    public double EstimatedDistanceKm { get; set; }
    public double EstimatedDurationMinutes { get; set; }
    public DispatchOrderStatus Status { get; set; } = DispatchOrderStatus.Pending;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public bool ExecuteRedistribution(DateTime now)
    {
        if (Status != DispatchOrderStatus.Pending)
            return false;

        Status = DispatchOrderStatus.InProgress;
        StartedAt = now;
        return true;
    }

    public bool CompleteOrder(DateTime now)
    {
        if (Status != DispatchOrderStatus.InProgress)
            return false;

        Status = DispatchOrderStatus.Completed;
        CompletedAt = now;
        return true;
    }
}