namespace SmartEMobilityHub.Core.Clock;

/// <summary>
/// Đồng hồ mô phỏng dùng chung cho app Sinh viên và Trung tâm điều hành (DC-54).
/// Giờ mô phỏng = mốc bắt đầu + thời gian thật đã trôi + phần đã tua.
/// - App Sinh viên: mốc bắt đầu = giờ thật lúc mở app, nên "Giờ thực" là giờ máy.
/// - Trung tâm điều hành chạy độc lập: mốc cố định 21/11/2026 09:41 để kịch bản demo lặp lại được.
/// Test truyền <see cref="FakeClock"/> làm "giờ thật" để điều khiển thời gian.
/// </summary>
public sealed class SimulationClock : IClock
{
    private readonly IClock _real;
    private readonly DateTime _realAtStart;
    private readonly DateTime _start;

    public SimulationClock(IClock? real = null, DateTime? start = null)
    {
        _real = real ?? new SystemClock();
        _realAtStart = _real.Now;
        _start = start ?? _realAtStart;
    }

    /// <summary>Phần thời gian đã tua thêm (nút +1/+5/+15 phút, chế độ ×10).</summary>
    public TimeSpan Offset { get; private set; } = TimeSpan.Zero;

    public DateTime Now => _start + (_real.Now - _realAtStart) + Offset;

    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta), "Chỉ được tua tới.");
        Offset += delta;
    }

    public void AdvanceSeconds(double seconds) => Advance(TimeSpan.FromSeconds(seconds));

    public void AdvanceMinutes(double minutes) => Advance(TimeSpan.FromMinutes(minutes));

    /// <summary>Tên cũ của Demo panel app Sinh viên.</summary>
    public void FastForward(double minutes) => AdvanceMinutes(minutes);

    /// <summary>Bỏ phần đã tua: quay về mốc bắt đầu + thời gian thật đã trôi.</summary>
    public void Reset() => Offset = TimeSpan.Zero;
}
