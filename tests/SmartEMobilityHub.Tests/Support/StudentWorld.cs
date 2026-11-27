using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Services;

namespace SmartEMobilityHub.Tests.Support;

/// <summary>
/// Một phiên app Sinh viên độc lập cho mỗi test, trên mạng lưới Core thật (DC-54): seed Core + đội xe của app,
/// đồng hồ điều khiển được. <see cref="Network"/> là chính phiên mà Trung tâm điều hành dùng khi liên thông.
/// </summary>
public sealed class StudentWorld
{
    public static DateTime Start => new(2026, 11, 21, 9, 41, 0);

    public FakeClock RealClock { get; }
    public StudentContext App { get; }

    private StudentWorld()
    {
        RealClock = new FakeClock(Start);
        App = new StudentContext(RealClock);
    }

    public static StudentWorld Create() => new();

    public NetworkSession Network => App.Network;

    public DateTime Now => App.Clock.Now;

    /// <summary>Thời gian thật trôi (giống một giây / một phút trôi qua khi app đang mở).</summary>
    public void Advance(double minutes) => RealClock.AdvanceMinutes(minutes);
}
