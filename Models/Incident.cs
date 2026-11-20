namespace SmartEMobilityHub.Models
{
    public class Incident
    {
        public string Code { get; init; } = "";
        public ReportObjectKind ObjectKind { get; init; }
        public string ObjectCode { get; init; } = "";
        public string HubId { get; init; } = "";
        public string ProblemType { get; init; } = "";
        public string Description { get; init; } = "";
        public IncidentStatus Status { get; set; }
        public IncidentSource Source { get; init; }
        public DateTime CreatedAt { get; init; }
        public int ReportCount { get; set; } = 1;
    }
}
