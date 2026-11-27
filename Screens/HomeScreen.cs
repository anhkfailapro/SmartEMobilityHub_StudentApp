using System.Windows;
using System.Windows.Media;
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

            // Thẻ tiêu đề gradient: lời chào, ô tìm điểm đến và tóm tắt mạng lưới gần thời gian thực.
            var go = Button("Tìm", ButtonVariant.Secondary, Search, true, Icons.Search);
            if (!Theme.IsDark) go.Background = Brushes.White;
            var available = store.Vehicles.Count(v => v.Status == VehicleStatus.Available);
            var freePoints = store.Hubs.Sum(h => h.FreePoints);
            var openHubs = store.Hubs.Count(h => !h.IsSuspended);
            var hello = Text($"Xin chào, {store.StudentName}", TextKind.Body, Theme.OnHero2);
            var title = Text("Bạn muốn đi đâu hôm nay?", TextKind.Title, Brushes.White);
            var stats = Columns(8,
                HeroStat(available.ToString(), "xe sẵn sàng"),
                HeroStat(openHubs.ToString(), "Hub hoạt động"),
                HeroStat(freePoints.ToString(), "cổng sạc trống"));
            var root = Stack(16, Hero(Stack(14,
                Stack(4, hello, title),
                Split(InputBox("Nhập điểm đến (VD: Thư viện Trung tâm)", _destination, v => _destination = v, false, Search), go),
                stats)));

            var trip = store.ActiveTrip;
            if (trip != null)
                Add(root, OnClick(Banner(Tone.Info, $"Bạn đang có chuyến đi với {trip.VehicleCode}. Nhấn để xem chuyến đi."), () => DemoApp.GoTab(NavTab.Trip)), 16);
            var held = DemoApp.Reservations.ActiveVehicleReservation;
            if (held != null)
                Add(root, OnClick(Banner(Tone.Warning, $"Đang giữ xe {held.VehicleCode} đến {DemoConfig.Time(held.HoldUntil)}. Nhấn để xem mã nhận xe."), () => Shell.Navigate(new ReservationScreen())), 16);

            Add(root, Section("Điểm đến phổ biến"), 4);
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

            var hubs = store.Hubs.OrderBy(h => h.DistanceKmTo(0, 0)).Take(_showAllHubs ? store.Hubs.Count : 3);
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

        private static UIElement HeroStat(string value, string label)
        {
            var b = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(38, 255, 255, 255)),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 8, 10, 8),
                Child = Stack(0, Text(value, TextKind.Heading, Brushes.White, false), Text(label, TextKind.Caption, Theme.OnHero2, false))
            };
            return b;
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
