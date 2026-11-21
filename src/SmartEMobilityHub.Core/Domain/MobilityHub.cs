namespace SmartEMobilityHub.Core.Domain;

/// <summary>
/// Một Hub trong mạng lưới (Bảng 23; §4.4 gọi là MobilityHub).
/// Các chỉ số utilization phục vụ ThresholdMonitor (Bảng 31).
/// </summary>
public sealed class MobilityHub
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string ShortName { get; init; } = "";
    public GeoPoint Location { get; init; } = new();

    public int ParkingCapacity { get; init; }
    public int OccupiedParkingSlots { get; set; }

    /// <summary>Giới hạn công suất Hub (kW) — UC-OP-04 Exception 3a.</summary>
    public double PowerLimitKw { get; set; }

    public ConnectionStatus Connection { get; set; } = ConnectionStatus.Online;
    public HubStatus Status { get; set; } = HubStatus.Online;
    public DateTime LastUpdatedAt { get; set; }

    public List<ChargingPoint> ChargingPoints { get; } = new();

    public int FreeParkingSlots => Math.Max(0, ParkingCapacity - OccupiedParkingSlots);

    /// <summary>Cổng còn dùng được (loại OFFLINE khỏi mẫu số khi tính utilization).</summary>
    public int UsableChargingPorts => ChargingPoints.Count(p => p.Status != DeviceStatus.Offline);

    public double ParkingUtilization =>
        ParkingCapacity <= 0 ? 0 : (double)OccupiedParkingSlots / ParkingCapacity;

    public double ChargingUtilization =>
        UsableChargingPorts <= 0
            ? 0
            : (double)ChargingPoints.Count(p => p.Status == DeviceStatus.Charging) / UsableChargingPorts;

    /// <summary>UC-SYS-03: max(tỷ lệ đỗ xe, tỷ lệ sạc) — Bảng 31.</summary>
    public double MaxUtilization => Math.Max(ParkingUtilization, ChargingUtilization);

    public MobilityHub Clone()
    {
        var clone = new MobilityHub
        {
            Id = Id,
            Name = Name,
            ShortName = ShortName,
            Location = Location.Clone(),
            ParkingCapacity = ParkingCapacity,
            OccupiedParkingSlots = OccupiedParkingSlots,
            PowerLimitKw = PowerLimitKw,
            Connection = Connection,
            Status = Status,
            LastUpdatedAt = LastUpdatedAt
        };
        foreach (var point in ChargingPoints) clone.ChargingPoints.Add(point.Clone());
        return clone;
    }
}
