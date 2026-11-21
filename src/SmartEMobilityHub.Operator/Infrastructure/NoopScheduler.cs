using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Operator.Infrastructure;

/// <summary>
/// Dashboard điều khiển chu kỳ quét từ UI (tránh thread nền sửa dữ liệu đang bind).
/// </summary>
public sealed class NoopScheduler : IScheduler
{
    public IDisposable Schedule(TimeSpan interval, Action onTick) => new Subscription();

    private sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
