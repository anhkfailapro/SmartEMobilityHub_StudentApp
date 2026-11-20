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
        private readonly DateTime _started = DateTime.Now;
        private bool _skip;
        private bool _done;

        public UnlockingScreen(VehicleReservation reservation)
        {
            _reservation = reservation;
        }

        public override string? Title => "Nhận xe";
        public override bool ShowBack => false;
        public override bool WantsTick => true;

        private double Elapsed => (DateTime.Now - _started).TotalSeconds;

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
            if (DemoApp.Toggles.UnlockTimeout)
                Add(root, Banner(Tone.Warning, "Demo: xe sẽ không phản hồi lệnh mở khóa."), 16);
            return root;
        }

        public override UIElement? BuildActions() =>
            DemoApp.Toggles.UnlockTimeout ? Button("Bỏ qua thời gian chờ (demo)", ButtonVariant.Outline, () => _skip = true) : null;

        public override void OnTick()
        {
            if (_done) return;
            if (!DemoApp.Toggles.UnlockTimeout && Elapsed >= DemoConfig.UnlockNormalSeconds)
            {
                _done = true;
                DemoApp.PickUp.ConfirmStart(_reservation);
                Shell.ResetTo(new TripScreen(true));
            }
            else if (DemoApp.Toggles.UnlockTimeout && (_skip || Elapsed >= DemoConfig.UnlockTimeoutSeconds))
            {
                _done = true;
                var incident = DemoApp.PickUp.ReceiveUnlockTimeout(_reservation);
                Shell.Replace(new UnlockFailedScreen(_reservation, incident));
            }
        }
    }
}
