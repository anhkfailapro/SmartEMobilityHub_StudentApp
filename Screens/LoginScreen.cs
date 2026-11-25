using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    /// <summary>
    /// Màn hình đăng nhập của app Sinh viên: MSSV + mật khẩu.
    /// Luồng thay thế: bỏ trống ô, sai thông tin (báo số lần còn lại), sai 5 lần → khoá 5 phút
    /// (tua "Thời gian demo" để mở khoá), tài khoản Operator → bị từ chối.
    /// </summary>
    public class LoginScreen : Screen
    {
        private string _username = "";
        private string _password = "";
        private string? _error;

        public override bool ShowBack => false;

        public override UIElement BuildContent()
        {
            var logo = IconTile(Icons.Bike, Tone.Info, 72);
            logo.HorizontalAlignment = HorizontalAlignment.Center;
            logo.Margin = new Thickness(0, 24, 0, 0);

            var root = Stack(16,
                logo,
                Centered(Text("Smart E-Mobility Hub", TextKind.Title)),
                Centered(Text("Đăng nhập bằng tài khoản sinh viên để đặt xe, đỗ xe và sạc xe.", TextKind.Body, Theme.Text2)),
                _error == null ? null : Banner(Tone.Danger, _error),
                Labeled("Mã số sinh viên", InputBox("VD: 2310001", _username, v => _username = v, onEnter: Submit)),
                Labeled("Mật khẩu", PasswordInput("Nhập mật khẩu", _password, v => _password = v, Submit)),
                Card(
                    Text("Tài khoản demo", TextKind.Subtitle),
                    Row("MSSV", AccountSeed.StudentUsername),
                    Row("Mật khẩu", AccountSeed.StudentPassword)));
            return root;
        }

        public override UIElement? BuildActions() => Button("Đăng nhập", ButtonVariant.Primary, Submit);

        private void Submit()
        {
            var result = DemoApp.Auth.Login(_username, _password, AccountRole.Student);
            if (!result.Success)
            {
                _error = result.Message;
                _password = "";
                Shell.Refresh();
                return;
            }

            _error = null;
            _password = "";
            DemoApp.OnLoggedIn(result.Account!);
        }
    }
}
