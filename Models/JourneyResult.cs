namespace SmartEMobilityHub.Models
{
    public class JourneyResult
    {
        public string Destination { get; init; } = "";
        public bool RouteAvailable { get; init; }
        public Hub? StartHub { get; init; }
        public Hub? EndHub { get; init; }
        public double WalkToStartKm { get; init; }
        public double EndToDestinationKm { get; init; }
        public double RideKm { get; init; }
        public List<Hub> HubsByStraightLine { get; init; } = new();
    }
}
