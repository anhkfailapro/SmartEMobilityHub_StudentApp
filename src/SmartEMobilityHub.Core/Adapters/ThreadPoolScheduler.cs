using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Core.Adapters;

/// <summary>Scheduler chạy nền bằng System.Threading.Timer (WPF dùng adapter DispatcherTimer riêng).</summary>
public sealed class ThreadPoolScheduler : IScheduler
{
    public IDisposable Schedule(TimeSpan interval, Action onTick)
    {
        return new Timer(_ => onTick(), null, interval, interval);
    }
}
