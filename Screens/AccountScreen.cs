using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class AccountScreen : Screen
    {
        public AccountScreen(DemoApp app) : base(app)
        {
        }

        public override string? Title => "Tài khoản";
        public override bool ShowBack => false;
        public override NavTab? Tab => NavTab.Account;

        public override UIElement BuildContent()
        {
            var store = App.Store;
            var vehicles = Card(store.PrivateVehicles.Select(p => (UIElement?)Row(p.Plate, p.Model)).ToArray());
            return Stack(16,
                Card(Text(store.StudentName, TextKind.Heading), Text($"MSSV {store.StudentId} · Sinh viên · Chủ xe điện cá nhân", TextKind.Body, Theme.Text2)),
                Section("Xe cá nhân đã đăng ký (chỉ xem)"),
                vehicles,
                Section("Gói tháng"),
                Card(Row("Phút miễn phí còn lại", $"{store.PlanFreeMinutes} phút", Theme.Primary)),
                Section("Giao diện"),
                Panel(Toggle("Chế độ tối", Theme.IsDark, v =>
                {
                    Theme.IsDark = v;
                    Shell.Refresh();
                })),
                Section("Hỗ trợ"),
                Card(Row("Hotline", DemoConfig.SupportHotline), Row("Giờ hỗ trợ", DemoConfig.SupportHours)),
                Button("Đăng xuất", ButtonVariant.DangerOutline, ConfirmLogout, glyph: Icons.Person));
        }

        private void ConfirmLogout()
        {
            Shell.ShowDialog(Tone.Warning, "Đăng xuất?",
                "Bạn sẽ quay về màn hình đăng nhập. Đặt chỗ và chuyến đi đang có vẫn được giữ.",
                new DialogAction("Đăng xuất", ButtonVariant.Danger, App.Logout),
                new DialogAction("Ở lại", ButtonVariant.Outline));
        }
    }
}
