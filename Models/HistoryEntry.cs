namespace SmartEMobilityHub.Models
{
    public class HistoryEntry
    {
        public DateTime Date { get; init; }
        public string Title { get; init; } = "";
        public string HubName { get; init; } = "";
        public string Duration { get; init; } = "";
        public string Fee { get; init; } = "";
        public string Status { get; init; } = "";
        public Trip? Trip { get; init; }
    }
}
