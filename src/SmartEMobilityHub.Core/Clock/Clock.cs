namespace SmartEMobilityHub.Core.Clock;

/// <summary>
/// Nguồn thời gian duy nhất của hệ thống. Service không được gọi DateTime.Now trực tiếp
/// (yêu cầu bắt buộc của PLAN §3) để test được các mốc 10/15 phút.
/// </summary>
public interface IClock
{
    DateTime Now { get; }
}

/// <summary>Đồng hồ thật — nơi duy nhất trong Core được phép đọc giờ hệ thống.</summary>
public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

/// <summary>Đồng hồ điều khiển được cho test: tua tới/lui không cần chờ.</summary>
public sealed class FakeClock : IClock
{
    private DateTime _now;

    public FakeClock(DateTime start) => _now = start;

    public DateTime Now => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);

    public void AdvanceMinutes(double minutes) => _now = _now.AddMinutes(minutes);

    public void AdvanceSeconds(double seconds) => _now = _now.AddSeconds(seconds);

    public void Set(DateTime value) => _now = value;
}

/// <summary>
/// Bọc một IClock và khuếch đại thời gian mô phỏng (demo auto-cancel 15 phút trong vài giây).
/// Giữ nguyên mốc gốc để thời điểm bắt đầu không nhảy.
/// </summary>
public sealed class ScaledClock : IClock
{
    private readonly IClock _inner;
    private readonly DateTime _origin;

    public ScaledClock(IClock inner, double scale, DateTime? origin = null)
    {
        if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale), "timeScale phải > 0.");
        _inner = inner;
        Scale = scale;
        _origin = origin ?? inner.Now;
    }

    public double Scale { get; }

    public DateTime Now
    {
        get
        {
            var elapsedTicks = _inner.Now.Ticks - _origin.Ticks;
            return _origin.AddTicks((long)(elapsedTicks * Scale));
        }
    }
}
