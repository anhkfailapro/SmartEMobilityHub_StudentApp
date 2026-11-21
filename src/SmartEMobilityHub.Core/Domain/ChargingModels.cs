namespace SmartEMobilityHub.Core.Domain;

/// <summary>
/// Yêu cầu sạc trong hàng chờ (Bảng 38) + các field bắt buộc để chạy UC-SYS-01
/// (vehicleId, hubId, pinned, assignedPortId — xem DESIGN_CHANGES DC-09).
/// </summary>
public sealed class ChargingRequest
{
    public string RequestId { get; init; } = "";
    public string VehicleId { get; init; } = "";
    public string HubId { get; init; } = "";
    public int BatteryLevel { get; set; }
    public DateTime NeededAt { get; set; }
    public double PeakDemand { get; set; } = ServiceConstants.DefaultPeakDemand;
    public double PriorityScore { get; set; }
    public int QueuePosition { get; set; }

    /// <summary>Xe bị Operator ghim thứ tự — không tính lại điểm (Bảng 37 manualReorder).</summary>
    public bool Pinned { get; set; }

    public string? AssignedPortId { get; set; }

    public ChargingRequest Clone() => new()
    {
        RequestId = RequestId,
        VehicleId = VehicleId,
        HubId = HubId,
        BatteryLevel = BatteryLevel,
        NeededAt = NeededAt,
        PeakDemand = PeakDemand,
        PriorityScore = PriorityScore,
        QueuePosition = QueuePosition,
        Pinned = Pinned,
        AssignedPortId = AssignedPortId
    };
}

/// <summary>Kết quả một lần lập lịch (Bảng 39).</summary>
public sealed class ChargingPlan
{
    public string HubId { get; init; } = "";
    public List<ChargingRequest> OrderedRequests { get; } = new();
    public double TotalPowerKw { get; set; }

    public ChargingPlan Clone()
    {
        var clone = new ChargingPlan { HubId = HubId, TotalPowerKw = TotalPowerKw };
        foreach (var request in OrderedRequests) clone.OrderedRequests.Add(request.Clone());
        return clone;
    }
}
