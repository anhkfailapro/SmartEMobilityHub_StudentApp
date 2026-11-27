using System.Windows;
using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Operator;
using SmartEMobilityHub.Services;
using OperatorLoginWindow = SmartEMobilityHub.Operator.LoginWindow;
using OperatorMainWindow = SmartEMobilityHub.Operator.MainWindow;

namespace SmartEMobilityHub.Launcher;

/// <summary>
/// Cửa sổ Trung tâm điều hành cho app Sinh viên (bản thật của <see cref="IOperatorConsole"/>).
/// Hai app dùng chung một <see cref="NetworkSession"/> nên không có bước đồng bộ; thông báo cho sinh viên về thay đổi
/// do Operator tạo ra nằm ở NetworkWatcher của app Sinh viên.
/// </summary>
public sealed class OperatorConsole : IOperatorConsole
{
    private OperatorMainWindow? _window;
    private AuthService? _operatorAuth;
    private OperatorDemo? _demo;

    public bool IsOpen => _window is not null;

    /// <summary>Mở cửa sổ đăng nhập Operator; đăng nhập đúng tài khoản Operator mới vào được bảng điều khiển (NFR 2.2.4).</summary>
    public void Open(NetworkSession session)
    {
        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        // Phiên đăng nhập riêng cho Operator để không đè phiên của sinh viên.
        _operatorAuth ??= CreateOperatorAuth(session);
        _operatorAuth.Logout();
        var login = new OperatorLoginWindow(_operatorAuth) { Owner = Application.Current.MainWindow };
        login.LoggedIn += account =>
        {
            var window = new OperatorMainWindow(DemoFor(session));
            window.SetCurrentUser(account);
            window.Closed += (_, _) => _window = null;
            _window = window;
            window.Show();
            login.Close();
        };
        login.Show();
    }

    /// <summary>App Sinh viên đặt lại demo: cửa sổ Operator đang mở chuyển sang mạng lưới mới.</summary>
    public void SessionChanged(NetworkSession session)
    {
        var demo = DemoFor(session);
        _window?.AttachDemo(demo);
    }

    /// <summary>Một <see cref="OperatorDemo"/> cho mỗi phiên, giữ nhật ký giữa các lần mở cửa sổ.</summary>
    private OperatorDemo DemoFor(NetworkSession session)
    {
        if (_demo is null || !ReferenceEquals(_demo.Session, session)) _demo = new OperatorDemo(session);
        return _demo;
    }

    private static AuthService CreateOperatorAuth(NetworkSession session)
    {
        var auth = new AuthService(session.Clock);
        AccountSeed.Apply(auth);
        return auth;
    }
}
