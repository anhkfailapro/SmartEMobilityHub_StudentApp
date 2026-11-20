namespace SmartEMobilityHub.Models
{
    public class AppNotification
    {
        public string Title { get; init; } = "";
        public string Body { get; init; } = "";
        public DateTime Time { get; init; }
        public Tone Tone { get; init; }
        public bool Read { get; set; }
    }
}
