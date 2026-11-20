namespace SmartEMobilityHub.Models
{
    public class ChargingEstimate
    {
        public Hub Hub { get; init; } = new();
        public int FreePoints { get; init; }
        public bool AllBusy { get; init; }
        public int WaitMinutes { get; init; }
        public int QueuePosition { get; init; }
        public double Score { get; init; }
        public PriorityLevel Priority { get; init; }
    }
}
