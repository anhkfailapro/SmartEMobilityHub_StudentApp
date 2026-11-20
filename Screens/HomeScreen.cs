using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Services;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class HomeScreen : Screen
    {
        private string _destination = "";
        private bool _showAllHubs;

        public override NavTab? Tab => NavTab.Home;

        public override UIElement BuildContent()
        {
            var store = DemoApp.Store;
            var root = Stack(16, Stack(4,
                Text($"Xin chào, {store.StudentName}", TextKind.Body, Theme.Text2),
                Text("Bạn muốn đi đâu hôm nay?", TextKind.Title)));

            var trip = store.ActiveTrip;
            if (trip != null)
                Add(root, OnClick(Banner(Tone.Info, $"Bạn đang có chuyến đi với {trip.VehicleCode}. Nhấn để xem chuyến đi."), () => DemoApp.GoTab(NavTab.Trip)), 16);
            var held = DemoApp.Reservations.ActiveVehicleReservation;
            if (held != null)
                Add(root, OnClick(Banner(Tone.Warning, $"Đang giữ xe {held.VehicleCode} đến {DemoConfig.Time(held.HoldUntil)}. Nhấn để xem mã nhận xe."), () => Shell.Navigate(new ReservationScreen())), 16);

            var go = Button("Tìm", ButtonVariant.Primary, Search, true, Icons.Search);
            Add(root, Split(InputBox("Nhập điểm đến (VD: Thư viện Trung tâm)", _destination, v => _destination = v), go), 16);
            Add(root, ChipGroup(JourneyPlanner.Destinations.Select(d => d.Name), "", n => n, n =>
            {
                _destination = n;
                Search();
            }), 8);

            var unread = DemoApp.Notifications.UnreadCount;
            Add(root, Columns(8,
                QuickAction(Icons.Parking, "Đặt chỗ đỗ xe", Tone.Info, () => Shell.Navigate(new ReserveParkingScreen())),
                QuickAction(Icons.Bolt, "Lên lịch sạc", Tone.Charging, () => Shell.Navigate(new ScheduleChargingScreen())),
                QuickAction(Icons.Clock, unread > 0 ? $"Hoạt động ({unread})" : "Hoạt động", Tone.Success, () => Shell.Navigate(new ActivityScreen(true)))), 8);

            Add(root, MapPreview(store.Hubs, h => Shell.Navigate(new VehicleListScreen(h.Id))), 16);
            Add(root, SectionWithLink("Hub gần bạn", _showAllHubs ? "Thu gọn" : "Xem tất cả", () =>
            {
                _showAllHubs = !_showAllHubs;
                Shell.Refresh();
            }), 16);

            var hubs = store.Hubs.OrderBy(h => h.DistanceKmTo(0, 0)).Take(_showAllHubs ? 10 : 3);
            foreach (var h in hubs)
            {
                var hub = h;
                Add(root, HubCard(hub.Name, ScreenHelpers.HubMeta(hub), ScreenHelpers.DistanceFromMe(hub), () => Shell.Navigate(new VehicleListScreen(hub.Id))), 12);
            }
            return root;
        }

        private void Search()
        {
            if (string.IsNullOrWhiteSpace(_destination))
            {
                Shell.ShowDialog(Tone.Info, "Nhập điểm đến", "Vui lòng nhập điểm đến hoặc chọn một gợi ý bên dưới ô tìm kiếm.");
                return;
            }
            Shell.Navigate(new JourneyResultScreen(_destination));
        }

        private static UIElement QuickAction(string glyph, string label, Tone tone, Action action)
        {
            var tile = IconTile(glyph, tone, 40);
            tile.HorizontalAlignment = HorizontalAlignment.Center;
            var p = Panel(Stack(8, tile, Centered(Text(label, TextKind.Caption))));
            p.Padding = new Thickness(8, 12, 8, 12);
            return OnClick(p, action);
        }
    }
}
