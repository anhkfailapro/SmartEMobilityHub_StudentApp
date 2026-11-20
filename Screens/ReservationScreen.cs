using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ReservationScreen : Screen
    {
        private readonly bool _justCreated;

        public ReservationScreen(bool justCreated = false)
        {
            _justCreated = justCreated;
        }

        public override string? Title => "Đặt chỗ của bạn";
        public override bool WantsTick => true;

        public override UIElement BuildContent()
        {
            var r = DemoApp.Reservations.ActiveVehicleReservation;
            if (r == null)
                return EmptyState(Icons.Calendar, "Không có đặt xe đang hoạt động", "Đặt chỗ có thể đã hết hạn, đã bị hủy hoặc bạn đã nhận xe.");

            var hub = DemoApp.Store.GetHub(r.HubId);
            var left = r.HoldUntil - DemoApp.Clock.Now;
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            var root = Stack(16);
            if (_justCreated) Add(root, Banner(Tone.Success, "Đặt xe thành công! Thông báo đã được gửi tới bạn."), 16);

            var countdown = new System.Windows.Controls.Border
            {
                Background = Theme.Primary,
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(20),
                Child = Stack(8,
                    Centered(Text("Thời gian giữ xe còn lại", TextKind.Body, Theme.OnPrimary)),
                    Centered(Text($"{(int)left.TotalMinutes:00}:{left.Seconds:00}", TextKind.Huge, Theme.OnPrimary)),
                    Progress(left.TotalMinutes / DemoConfig.HoldMinutes, Theme.OnPrimary, Theme.PrimarySubtle, 6))
            };
            Add(root, countdown, 16);
            Add(root, Card(
                Centered(Text("Mã nhận xe (Pick-up code)", TextKind.Body, Theme.Text2)),
                Centered(Text(r.PickUpCode.Replace("-", " - "), TextKind.Big, Theme.Primary)),
                Qr(r.PickUpCode),
                Centered(Text($"Xe {r.VehicleCode} · {hub.Name} · Slot {r.Slot}", TextKind.Caption, Theme.Text2))), 16);
            if (r.IsPickingUp)
                Add(root, Banner(Tone.Info, "Bạn đang nhận xe nên thời gian giữ được gia hạn cho đến khi mở khóa xong."), 16);
            return root;
        }

        public override UIElement? BuildActions()
        {
            var r = DemoApp.Reservations.ActiveVehicleReservation;
            if (r == null) return Button("Tìm xe khác", ButtonVariant.Primary, ScreenHelpers.GoHome);
            var id = r.Id;
            return Stack(12,
                Button("Quét QR để nhận xe", ButtonVariant.Primary, () => Shell.Navigate(new ScanQrScreen()), true, Icons.Camera),
                Columns(12,
                    Button("Nhập mã thủ công", ButtonVariant.Outline, () => Shell.Navigate(new ManualCodeScreen())),
                    Button("Hủy đặt chỗ", ButtonVariant.DangerOutline, () => Shell.Navigate(new CancelConfirmScreen(BookingKind.Vehicle, id)))));
        }
    }
}
