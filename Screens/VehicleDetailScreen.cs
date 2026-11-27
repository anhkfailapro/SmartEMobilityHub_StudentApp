using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class VehicleDetailScreen : Screen
    {
        private readonly string _code;

        public VehicleDetailScreen(DemoApp app, string code) : base(app)
        {
            _code = code;
        }

        public override string? Title => "Chi tiết xe";

        public override UIElement BuildContent()
        {
            var v = App.Vehicles.GetDetails(_code)!;
            var now = App.Clock.Now;
            var stale = v.IsStale(now);
            var readOnly = App.Vehicles.IsReadOnly(v);
            var (label, tone) = DemoConfig.VehicleStatusLabel(v.Status);
            var hubName = v.HubId != null ? App.Store.GetHub(v.HubId).Name : "Đang di chuyển";

            var root = Stack(16);
            if (stale)
                Add(root, Banner(Tone.Warning, $"Dữ liệu cũ: trạng thái cập nhật {(int)(now - v.LastUpdate).TotalMinutes} phút trước, có thể không còn chính xác."), 16);

            var image = new System.Windows.Controls.Border { Height = 96, CornerRadius = new CornerRadius(12), Background = Theme.SkySubtle, Child = Icon(Icons.Bike, 56, Theme.Sky) };
            Add(root, Card(image, Split(Text(v.Code, TextKind.Title), Badge(label, tone)), Text($"{v.TypeLabel} · {hubName} · Slot {v.Slot}", TextKind.Body, Theme.Text2)), 16);
            Add(root, Columns(12,
                StatTile(Icons.Bolt, $"{v.Battery}%", "Mức pin", Tone.Success),
                StatTile(Icons.Pin, $"~{v.RangeKm} km", "Quãng đường"),
                StatTile(Icons.Clock, $"{DemoConfig.HoldMinutes} phút", "Giữ xe", Tone.Warning)), 16);
            Add(root, Card(Split(Text("Mức pin", TextKind.Subtitle), Text($"{v.Battery}%", TextKind.Subtitle, null, false)), BatteryBar(v.Battery)), 16);
            Add(root, Card(
                Text("Thông tin", TextKind.Subtitle),
                Row("Tình trạng", v.Status == VehicleStatus.Available ? "Hoạt động tốt" : label, v.Status == VehicleStatus.Available ? null : Theme.Danger),
                Row("Loại xe", v.TypeLabel),
                Row("Giá thuê", $"{DemoConfig.Money(DemoConfig.RentalPricePerMinute)} / phút"),
                Row("Cập nhật lần cuối", DemoConfig.Ago(v.LastUpdate, now), stale ? Theme.Warning : null)), 16);
            if (readOnly)
                Add(root, Banner(Tone.Danger, "Xe đang hỏng, đang kiểm tra, đang sạc hoặc đã được người khác đặt. Trang chỉ xem, không thể đặt trước."), 16);
            if (v.HubId != null && v.Battery < 50 && v.Status != VehicleStatus.UnderCheck)
            {
                var hubId = v.HubId;
                Add(root, Button("Yêu cầu sạc cho xe này", ButtonVariant.Charging, () => Shell.Navigate(new ChargingRequestScreen(App, _code, hubId)), true, Icons.Bolt), 16);
            }
            return root;
        }

        public override UIElement? BuildActions()
        {
            var v = App.Vehicles.GetDetails(_code)!;
            return Columns(12,
                Button("Quay lại", ButtonVariant.Outline, Shell.Back),
                Button("Đặt trước", ButtonVariant.Primary, Reserve, !App.Vehicles.IsReadOnly(v)));
        }

        private void Reserve()
        {
            var error = App.Reservations.CheckReserve(_code);
            if (error != "")
            {
                ReservationDialogs.ShowReserveError(App, error, _code);
                return;
            }
            Shell.Navigate(new ConfirmReservationScreen(App, _code));
        }
    }
}
