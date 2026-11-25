using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;

namespace SmartEMobilityHub.Operator;

/// <summary>
/// Cửa sổ đăng nhập trước khi mở Trung tâm điều hành.
/// Chỉ nhận tài khoản <see cref="AccountRole.Operator"/>; sai 5 lần thì khoá 5 phút.
/// </summary>
public partial class LoginWindow : Window
{
    private readonly AuthService _auth;

    public event Action<UserAccount>? LoggedIn;

    public LoginWindow(AuthService auth)
    {
        _auth = auth;
        Themes.OperatorTheme.EnsureLoaded();
        InitializeComponent();
        DemoHint.Text = $"Tài khoản demo: {AccountSeed.OperatorUsername} · mật khẩu {AccountSeed.OperatorPassword}";
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var result = _auth.Login(UsernameBox.Text, PasswordBox.Password, AccountRole.Operator);
        if (!result.Success)
        {
            ErrorText.Text = result.Message;
            ErrorPanel.Visibility = Visibility.Visible;
            PasswordBox.Clear();
            if (result.Error == LoginError.MissingCredentials && UsernameBox.Text.Trim().Length == 0) UsernameBox.Focus();
            else PasswordBox.Focus();
            return;
        }

        ErrorPanel.Visibility = Visibility.Collapsed;
        LoggedIn?.Invoke(result.Account!);
    }
}
