using System.Windows.Controls;
using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Screens;
using SmartEMobilityHub.Services;

namespace SmartEMobilityHub.Tests.Ui.Support;

/// <summary>Console Operator giả: ghi lại phiên mạng lưới mà app Sinh viên đưa cho nó.</summary>
public sealed class FakeOperatorConsole : IOperatorConsole
{
    public bool IsOpen { get; private set; }
    public List<NetworkSession> Opened { get; } = new();
    public List<NetworkSession> Sessions { get; } = new();

    public void Open(NetworkSession session)
    {
        IsOpen = true;
        Opened.Add(session);
    }

    public void SessionChanged(NetworkSession session) => Sessions.Add(session);
}

/// <summary>
/// App Sinh viên dựng trong test: không cửa sổ, không nhịp 1 giây, đồng hồ điều khiển được, đã đăng nhập tài khoản demo.
/// Ghi lại loại màn hình đã dựng để tính độ phủ màn hình.
/// </summary>
public sealed class StudentHarness
{
    public static DateTime Start => new(2026, 11, 21, 9, 41, 0);

    public StudentHarness(IOperatorConsole? console = null)
    {
        RealClock = new FakeClock(Start);
        App = new DemoApp(console, new StudentContext(RealClock));
        Phone = new Border();
        Panel = new Border();
        App.Start(Phone, Panel, requireLogin: false, startTimer: false);
    }

    public FakeClock RealClock { get; }
    public DemoApp App { get; }
    public Border Phone { get; }
    public Border Panel { get; }

    /// <summary>Đưa màn hình lên khung điện thoại (dựng nội dung và nút) và ghi nhận đã dựng được.</summary>
    public T Show<T>(T screen) where T : Screen
    {
        App.Shell.Navigate(screen);
        Assert.True(ReferenceEquals(App.Shell.Current, screen), $"{typeof(T).Name} là màn hình hiện tại");
        Assert.True(Phone.Child is not null, $"{typeof(T).Name} vẽ ra nội dung");
        ScreenCoverage.Rendered.Add(screen.GetType());
        return screen;
    }
}

/// <summary>Độ phủ màn hình: số lớp Screen của app Sinh viên đã được dựng ít nhất một lần trong bộ test.</summary>
public static class ScreenCoverage
{
    public static HashSet<Type> Rendered { get; } = new();

    public static IReadOnlyList<Type> AllScreens =>
        typeof(Screen).Assembly.GetTypes()
            .Where(t => typeof(Screen).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<Type> Missing => AllScreens.Where(t => !Rendered.Contains(t)).ToList();
}
