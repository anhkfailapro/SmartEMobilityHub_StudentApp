using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class UnlockingScreen : Screen
    {
        private readonly VehicleReservation _reservation;
        // Dùng đồng hồ demo (giờ thật + phần tua) để nút "+1 phút" ở Demo panel cũng tác động tới thời gian chờ mở khoá.
        private readonly DateTime _started;
        private bool _skip;
        private bool _done;

        public UnlockingScreen(DemoApp app, VehicleReservation reservation) : base(app)
        {
            _reservation = reservation;
            _started = App.Clock.Now;
        }

        public override string? Title => "Nhận xe";
        public override bool ShowBack => false;
        public override bool WantsTick => true;

        private double Elapsed => Math.Max(0, (App.Clock.Now - _started).TotalSeconds);

        public override UIElement BuildContent()
        {
            var seconds = Math.Min(DemoConfig.UnlockTimeoutSeconds, (int)Elapsed);
            var tile = IconTile(Icons.Bike, Tone.Info, 72);
            tile.HorizontalAlignment = HorizontalAlignment.Center;
            var root = Stack(16,
                tile,
                Centered(Text($"Đang mở khóa xe {_reservation.VehicleCode}…", TextKind.Heading)),
                Centered(Text($"Đã xác thực sinh viên và mã xe. Đang chờ xe xác nhận mở khóa (tối đa {DemoConfig.UnlockTimeoutSeconds} giây).", TextKind.Body, Theme.Text2)),
                Progress((double)seconds / DemoConfig.UnlockTimeoutSeconds, Theme.Primary, Theme.SurfaceAlt),
                Centered(Text($"{seconds} / {DemoConfig.UnlockTimeoutSeconds} giây", TextKind.Caption, Theme.Text2)));
            root.Margin = new Thickness(0, 40, 0, 0);
            if (App.Toggles.UnlockTimeout)
                Add(root, Banner(Tone.Warning, "Demo: xe sẽ không phản hồi lệnh mở khóa."), 16);
            return root;
        }

        public override UIElement? BuildActions() =>
            App.Toggles.UnlockTimeout ? Button("Bỏ qua thời gian chờ (demo)", ButtonVariant.Outline, () => _skip = true) : null;

        public override void OnTick()
        {
            if (_done) return;
            if (!App.Toggles.UnlockTimeout && Elapsed >= DemoConfig.UnlockNormalSeconds)
            {
                _done = true;
                if (App.PickUp.ConfirmStart(_reservation) != null)
                {
                    Shell.ResetTo(new TripScreen(App, true));
                    return;
                }
                // Mạng lưới không cho nhận xe nữa (ví dụ Operator vừa tạm dừng Hub và huỷ đặt chỗ).
                Shell.ResetTo(new ReservationScreen(App));
                Shell.ShowDialog(Tone.Danger, "Không nhận được xe",
                    _reservation.CancelReason is { } reason ? $"Đặt xe {_reservation.VehicleCode} đã bị hủy: {reason}." : "Đặt xe không còn hiệu lực.",
                    new DialogAction("Đóng", ButtonVariant.Outline, () => { }));
            }
            else if (App.Toggles.UnlockTimeout && (_skip || Elapsed >= DemoConfig.UnlockTimeoutSeconds))
            {
                _done = true;
                var incident = App.PickUp.ReceiveUnlockTimeout(_reservation);
                Shell.Replace(new UnlockFailedScreen(App, _reservation, incident));
            }
        }
    }
}
