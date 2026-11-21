namespace SmartEMobilityHub.Core.Domain;

/// <summary>Cổng sạc tại Hub (Bảng 23: availableChargingPorts; §6.2 Sub 2: sơ đồ 8 cổng).</summary>
public sealed class ChargingPoint
{
    /// <summary>Mã cổng dạng CS-08-04.</summary>
    public string Id { get; init; } = "";
    public string HubId { get; init; } = "";
    public int Number { get; init; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Available;
    public double PowerKw { get; init; } = 7.0;
    public double TemperatureC { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string? AssignedVehicleCode { get; set; }

    public ChargingPoint Clone() => new()
    {
        Id = Id,
        HubId = HubId,
        Number = Number,
        Status = Status,
        PowerKw = PowerKw,
        TemperatureC = TemperatureC,
        LastUpdatedAt = LastUpdatedAt,
        AssignedVehicleCode = AssignedVehicleCode
    };
}
