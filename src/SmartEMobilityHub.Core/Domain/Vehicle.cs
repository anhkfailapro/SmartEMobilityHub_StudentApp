namespace SmartEMobilityHub.Core.Domain;

/// <summary>Phương tiện điện dùng chung (Bảng 17, Bảng 22).</summary>
public sealed class Vehicle
{
    public string Code { get; init; } = "";
    public VehicleType Type { get; init; }
    public string Model { get; init; } = "";
    public string? HubId { get; set; }
    public string Slot { get; set; } = "";

    public int BatteryLevel { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Available;
    public ConnectionStatus Connection { get; set; } = ConnectionStatus.Online;
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>Cổng sạc đang gán (nếu đang CHARGING) — Bảng 37 allocatePower.</summary>
    public string? AssignedPortId { get; set; }

    public Vehicle Clone() => new()
    {
        Code = Code,
        Type = Type,
        Model = Model,
        HubId = HubId,
        Slot = Slot,
        BatteryLevel = BatteryLevel,
        Status = Status,
        Connection = Connection,
        LastUpdatedAt = LastUpdatedAt,
        AssignedPortId = AssignedPortId
    };
}
