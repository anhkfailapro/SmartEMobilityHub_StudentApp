using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ChargingQueuedScreen : Screen
    {
        private readonly string _requestId;

        public ChargingQueuedScreen(DemoApp app, string requestId) : base(app)
        {
            _requestId = requestId;
        }

        public override string? Title => "Yêu cầu sạc";
        public override bool WantsTick => true;

        public override UIElement BuildContent()
        {
            var r = App.Store.ChargingRequests.First(x => x.Id == _requestId);
            var hub = App.Store.GetHub(r.HubId);
            var assigned = r.State == ChargingRequestState.Assigned;
            var cancelled = r.State == ChargingRequestState.Cancelled;
            var tile = IconTile(Icons.Bolt, cancelled ? Tone.Neutral : assigned ? Tone.Success : Tone.Charging, 72);
            tile.HorizontalAlignment = HorizontalAlignment.Center;
            var title = cancelled ? "Yêu cầu đã bị hủy" : assigned ? "Đã có cổng sạc" : "Đã vào hàng đợi sạc";
            var body = assigned
                ? $"Cổng sạc #{r.AssignedPoint} tại {hub.Name} đã sẵn sàng cho {r.VehicleCode}."
                : $"Yêu cầu sạc cho {r.VehicleCode} đã được thêm vào hàng chờ của {hub.Name}.";
            var root = Stack(16, tile, Centered(Text(title, TextKind.Title)), Centered(Text(body, TextKind.Body, Theme.Text2)),
                Card(
                    Row("Xe", r.VehicleCode),
                    Row("Hub", hub.ShortName),
                    assigned ? Row("Cổng sạc", $"#{r.AssignedPoint}", Theme.Success) : Row("Vị trí hàng chờ", $"#{r.QueuePosition}", Theme.Charging),
                    Row("Thời gian chờ ước tính", assigned ? "Sẵn sàng" : $"~{r.WaitMinutes} phút"),
                    Row("Mức ưu tiên", $"{DemoConfig.PriorityLabel(DemoConfig.PriorityFromScore(r.Score))} ({r.Score:0})")));
            if (!assigned && !cancelled)
                Add(root, Banner(Tone.Charging, "Bạn sẽ nhận thông báo khi có cổng sạc được cấp. (Demo: dùng Demo panel → \"Cấp cổng sạc\".)"), 16);
            return root;
        }

        public override UIElement? BuildActions()
        {
            var inTrip = App.Billing.ActiveTrip != null;
            return Columns(12,
                Button(inTrip ? "Về chuyến đi" : "Về trang chủ", ButtonVariant.Outline, () => App.GoTab(inTrip ? NavTab.Trip : NavTab.Home)),
                Button("Đặt chỗ của tôi", ButtonVariant.Primary, () => App.GoTab(NavTab.Booking)));
        }
    }
}
