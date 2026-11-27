using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ParkingDetailScreen : Screen
    {
        private readonly string _id;
        private readonly bool _justCreated;

        public ParkingDetailScreen(DemoApp app, string id, bool justCreated) : base(app)
        {
            _id = id;
            _justCreated = justCreated;
        }

        public override string? Title => "Chỗ đỗ của bạn";

        public override UIElement BuildContent()
        {
            var p = App.Store.ParkingReservations.First(x => x.Id == _id);
            var hub = App.Store.GetHub(p.HubId);
            var pv = App.Store.GetPrivateVehicle(p.PrivateVehicleId);
            var (label, tone) = DemoConfig.ReservationStateLabel(p.State, false);
            var root = Stack(16);
            if (_justCreated) Add(root, Banner(Tone.Success, "Đặt chỗ đỗ xe thành công! Thông báo đã được gửi tới bạn."), 16);
            Add(root, Card(
                Centered(Text($"Chỗ đỗ {p.SlotNumber}", TextKind.Title)),
                Centered(Text("Mã QR vào bãi", TextKind.Body, Theme.Text2)),
                Qr(p.EntryCode),
                Centered(Text(p.EntryCode, TextKind.Caption, Theme.Text2))), 16);
            Add(root, Card(
                Row("Hub", hub.ShortName),
                Row("Xe", $"{pv.Plate} · {pv.Model}"),
                Row("Thời gian", $"{p.Start:dd/MM} · {DemoConfig.Time(p.Start)}–{DemoConfig.Time(p.End)}"),
                Row("Trạng thái", label, Theme.ToneBrushes(tone).Foreground)), 16);
            return root;
        }

        public override UIElement? BuildActions() => Columns(12,
            Button("Về trang chủ", ButtonVariant.Outline, GoHome),
            Button("Đặt chỗ của tôi", ButtonVariant.Primary, () => App.GoTab(NavTab.Booking)));
    }
}
