using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Tests.Support;

/// <summary>Lịch thủ công: test gọi <see cref="Fire"/> thay vì chờ 30 giây thật.</summary>
public sealed class ManualScheduler : IScheduler
{
    private Action? _onTick;

    public TimeSpan Interval { get; private set; }
    public bool IsScheduled => _onTick is not null;

    public IDisposable Schedule(TimeSpan interval, Action onTick)
    {
        Interval = interval;
        _onTick = onTick;
        return new Subscription(this);
    }

    public void Fire() => _onTick?.Invoke();

    private sealed class Subscription : IDisposable
    {
        private readonly ManualScheduler _owner;

        public Subscription(ManualScheduler owner) => _owner = owner;

        public void Dispose() => _owner._onTick = null;
    }
}
