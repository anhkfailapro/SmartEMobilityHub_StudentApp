using System.Windows;
using System.Windows.Media;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class TripScreen : Screen
    {
        private readonly bool _justStarted;

        public TripScreen(bool justStarted = false)
        {
            _justStarted = justStarted;
        }

        public override string? Title => DemoApp.Billing.ActiveTrip != null ? "Chuyến đi đang diễn ra" : "Chuyến đi";
        public override bool ShowBack => false;
        public override NavTab? Tab => NavTab.Trip;
        public override bool WantsTick => DemoApp.Billing.ActiveTrip != null;

        public override UIElement BuildContent()
        {
            var billing = DemoApp.Billing;
            var trip = billing.ActiveTrip;
            if (trip == null)
            {
                var empty = EmptyState(Icons.Bike, "Bạn chưa có chuyến đi nào", "Đặt xe và nhận xe để bắt đầu chuyến đi.");
                Add(empty, Button("Tìm xe", ButtonVariant.Primary, ScreenHelpers.GoHome), 16);
                return empty;
            }

            var v = DemoApp.Store.GetVehicle(trip.VehicleCode)!;
            var hub = DemoApp.Store.GetHub(trip.StartHubId);
            var root = Stack(16);
            if (_justStarted) Add(root, Banner(Tone.Success, "Mở khóa thành công. Chuyến đi đã bắt đầu."), 16);

            var timer = new System.Windows.Controls.Border
            {
                Background = Theme.Primary,
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(20),
                Child = Stack(12,
                    Centered(Text("Thời gian đã đi", TextKind.Body, Theme.OnPrimary)),
                    Centered(Text(ScreenHelpers.Elapsed(DemoApp.Clock.Now - trip.StartTime), TextKind.Huge, Theme.OnPrimary)),
                    Columns(8,
                        Tile($"{billing.CurrentBattery(trip)}%", "Pin"),
                        Tile(DemoConfig.Km(billing.CurrentDistanceKm(trip)), "Quãng đường"),
                        Tile(DemoConfig.Money(billing.CurrentFee(trip)), "Phí hiện tại")))
            };
            Add(root, timer, 16);
            Add(root, Card(
                ScreenHelpers.VehicleHeader(v, $"{v.TypeLabel} · Nhận lúc {DemoConfig.Time(trip.StartTime)} tại {hub.Name}"),
                BatteryBar(billing.CurrentBattery(trip))), 16);
            if (trip.ExtraFreeMinutes > 0)
                Add(root, Banner(Tone.Success, $"Bạn được cộng thêm {trip.ExtraFreeMinutes} phút miễn phí để đến Hub còn chỗ trống."), 16);
            Add(root, RoutePreview(), 16);
            Add(root, Columns(12,
                Button("Yêu cầu sạc", ButtonVariant.Charging, () => Shell.Navigate(new ChargingRequestScreen(trip.VehicleCode, trip.StartHubId)), true, Icons.Bolt),
                Button("Báo lỗi", ButtonVariant.DangerSubtle, () => Shell.Navigate(new ReportProblemScreen(ReportObjectKind.Vehicle, trip.VehicleCode)), true, Icons.Warning)), 16);
            Add(root, Button("Trả xe", ButtonVariant.Primary, () => Shell.Navigate(new ReturnVehicleScreen())), 12);
            return root;
        }

        private static UIElement Tile(string value, string label)
        {
            var b = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(6, 10, 6, 10),
                Child = Stack(2, Centered(Text(value, TextKind.Subtitle, Theme.OnPrimary)), Centered(Text(label, TextKind.Caption, Theme.OnPrimary)))
            };
            return b;
        }

        private static UIElement RoutePreview()
        {
            var canvas = new System.Windows.Controls.Canvas { Height = 120 };
            var line = new System.Windows.Shapes.Polyline
            {
                Stroke = Theme.Primary,
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 1, 2 },
                StrokeDashCap = PenLineCap.Round,
                Points = new PointCollection { new Point(30, 95), new Point(90, 70), new Point(150, 50), new Point(220, 62), new Point(300, 30) }
            };
            canvas.Children.Add(line);
            AddPin(canvas, 18, 83, Theme.Success);
            AddPin(canvas, 288, 18, Theme.Danger);
            return new System.Windows.Controls.Border { CornerRadius = new CornerRadius(16), Background = Theme.SkySubtle, Child = canvas };
        }

        private static void AddPin(System.Windows.Controls.Canvas canvas, double x, double y, Brush color)
        {
            var pin = new System.Windows.Controls.Border { Width = 24, Height = 24, CornerRadius = new CornerRadius(12), Background = Theme.Surface, Child = Icon(Icons.Pin, 12, color) };
            System.Windows.Controls.Canvas.SetLeft(pin, x);
            System.Windows.Controls.Canvas.SetTop(pin, y);
            canvas.Children.Add(pin);
        }
    }
}
