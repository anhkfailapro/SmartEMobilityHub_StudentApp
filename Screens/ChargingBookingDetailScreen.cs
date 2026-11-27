using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ChargingBookingDetailScreen : Screen
    {
        private readonly string _id;
        private readonly bool _justCreated;

        public ChargingBookingDetailScreen(DemoApp app, string id, bool justCreated) : base(app)
        {
            _id = id;
            _justCreated = justCreated;
        }

        public override string? Title => "Lịch sạc của bạn";

        public override UIElement BuildContent()
        {
            var s = App.Store.ChargingSessions.First(x => x.Id == _id);
            var hub = App.Store.GetHub(s.HubId);
            var pv = App.Store.GetPrivateVehicle(s.PrivateVehicleId);
            var (label, tone) = DemoConfig.SessionStateLabel(s.State);
            var root = Stack(16);
            if (_justCreated) Add(root, Banner(Tone.Success, "Đặt lịch sạc thành công! Cổng sạc và chỗ đỗ đã được giữ cho bạn."), 16);
            Add(root, Card(
                Split(Text("Lịch sạc", TextKind.Subtitle), Badge(label, tone)),
                Row("Hub", hub.ShortName),
                Row("Xe", $"{pv.Plate} · {pv.Model}"),
                Row("Cổng sạc", $"#{s.PointNumber}"),
                Row("Chỗ đỗ", s.SlotNumber),
                Row("Thời gian", $"{s.Start:dd/MM} · {DemoConfig.Time(s.Start)}–{DemoConfig.Time(s.End)}"),
                Row("Pin mục tiêu", $"{s.TargetBattery}%"),
                Row("Chi phí ước tính", $"~{DemoConfig.Money(App.Booking.EstimateCost(s.DurationMinutes))}")), 16);
            Add(root, Banner(Tone.Charging, $"Bạn sẽ nhận thông báo nhắc trước {DemoConfig.ReminderMinutesBefore} phút. Nếu cổng sạc ngưng hoạt động trước giờ bắt đầu, lịch sẽ được chuyển sang cổng khác hoặc bị hủy và bạn sẽ được thông báo."), 16);
            return root;
        }

        public override UIElement? BuildActions() => Columns(12,
            Button("Về trang chủ", ButtonVariant.Outline, GoHome),
            Button("Đặt chỗ của tôi", ButtonVariant.Primary, () => App.GoTab(NavTab.Booking)));
    }
}
