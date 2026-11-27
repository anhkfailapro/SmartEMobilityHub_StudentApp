using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Core.Adapters;

/// <summary>
/// Scheduler không tự chạy: chu kỳ quét do người gọi kích hoạt (UI gọi <c>TickNow()</c>
/// từ DispatcherTimer, test gọi <see cref="Fire"/>). Mọi thao tác trên <c>NetworkStore</c>
/// vì thế chạy trên một luồng duy nhất — <c>NetworkStore</c> không an toàn đa luồng (DC-47).
/// </summary>
public sealed class ManualScheduler : IScheduler
{
    private Action? _onTick;

    /// <summary>Đang có lịch đăng ký (Start đã gọi, chưa Stop).</summary>
    public bool IsScheduled => _onTick is not null;

    public IDisposable Schedule(TimeSpan interval, Action onTick)
    {
        _onTick = onTick;
        return new Subscription(this, onTick);
    }

    /// <summary>Chạy một chu kỳ như khi hết <c>interval</c>. Trả false nếu chưa có lịch.</summary>
    public bool Fire()
    {
        var tick = _onTick;
        if (tick is null) return false;
        tick();
        return true;
    }

    private sealed class Subscription(ManualScheduler owner, Action onTick) : IDisposable
    {
        public void Dispose()
        {
            if (owner._onTick == onTick) owner._onTick = null;
        }
    }
}
