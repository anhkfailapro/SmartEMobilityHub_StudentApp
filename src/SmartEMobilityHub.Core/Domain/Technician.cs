namespace SmartEMobilityHub.Core.Domain;

/// <summary>Kỹ thuật viên bảo trì (Bảng 46/47).</summary>
public sealed class Technician
{
    public string TechId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Team { get; init; } = "";
    public bool Available { get; set; }
    public GeoPoint Location { get; init; } = new();

    /// <summary>Bảng 47: chỉ nhận việc khi đang sẵn sàng.</summary>
    public bool IsAvailable() => Available;

    public double DistanceToHub(MobilityHub hub) => Location.DistanceKmTo(hub.Location);

    public Technician Clone() => new()
    {
        TechId = TechId,
        Name = Name,
        Team = Team,
        Available = Available,
        Location = Location.Clone()
    };
}
