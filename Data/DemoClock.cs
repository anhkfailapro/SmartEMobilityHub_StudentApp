using SmartEMobilityHub.Core.Clock;

namespace SmartEMobilityHub.Data
{
    public class DemoClock : IClock
    {
        public TimeSpan Offset { get; private set; } = TimeSpan.Zero;

        public DateTime Now => DateTime.Now + Offset;

        public void FastForward(double minutes) => Offset += TimeSpan.FromMinutes(minutes);

        public void Reset() => Offset = TimeSpan.Zero;
    }
}
