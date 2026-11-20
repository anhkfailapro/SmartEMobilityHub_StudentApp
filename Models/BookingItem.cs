namespace SmartEMobilityHub.Models
{
    public class BookingItem
    {
        public BookingKind Kind { get; init; }
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string HubName { get; init; } = "";
        public List<(string Label, string Value)> Rows { get; } = new();
        public string StatusLabel { get; init; } = "";
        public Tone StatusTone { get; init; }
        public DateTime? Start { get; init; }
        public bool Started { get; init; }
        public bool CanChangeTime { get; init; }
        public bool IsActive { get; init; }
    }
}
