using System.Windows;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Operator.Themes;

namespace SmartEMobilityHub.Operator;

public partial class App : Application
{
    /// <summary>Phiên đăng nhập sống suốt ứng dụng (đặt lại demo không làm đăng xuất).</summary>
    public static AuthService Auth { get; } = CreateAuth();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        OperatorTheme.EnsureLoaded();

        // Chế độ chụp màn hình: tự đăng nhập tài khoản demo và mở thẳng bảng điều khiển.
        if (e.Args.Contains("--capture"))
        {
            var demo = Auth.Login(AccountSeed.OperatorUsername, AccountSeed.OperatorPassword, AccountRole.Operator);
            OpenDashboard(demo.Account!);
            return;
        }

        ShowLogin();
    }

    /// <summary>Mở cửa sổ đăng nhập; đăng nhập xong thì mở bảng điều khiển và đóng cửa sổ này.</summary>
    public void ShowLogin()
    {
        var login = new LoginWindow(Auth);
        login.LoggedIn += account =>
        {
            OpenDashboard(account);
            login.Close();
        };
        MainWindow = login;
        login.Show();
    }

    /// <summary>Đăng xuất: mở lại màn hình đăng nhập trước rồi mới đóng bảng điều khiển để app không thoát.</summary>
    public void Logout(Window dashboard)
    {
        Auth.Logout();
        ShowLogin();
        dashboard.Close();
    }

    private void OpenDashboard(UserAccount account)
    {
        var window = new MainWindow();
        window.SetCurrentUser(account);
        MainWindow = window;
        window.Show();
    }

    private static AuthService CreateAuth()
    {
        var auth = new AuthService(new SystemClock());
        AccountSeed.Apply(auth);
        return auth;
    }
}
