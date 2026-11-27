using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Ui
{
    /// <summary>Bộ dựng giao diện của app Sinh viên — phần Thành phần theo nghiệp vụ: HubCard, VehicleCard, StatTile, EmptyState, MapPreview.</summary>
    public static partial class UiKit
    {
        public static Border HubCard(string name, string meta, string distance, Action? onClick)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tile = IconTile(Icons.Pin, Tone.Info);
            tile.Margin = new Thickness(0, 0, 12, 0);
            var info = Stack(4, Text(name, TextKind.Subtitle), Text(meta, TextKind.Caption, Theme.Text2));
            Grid.SetColumn(info, 1);
            var dist = Text(distance, TextKind.Subtitle, Theme.Primary, false);
            dist.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(dist, 2);
            g.Children.Add(tile);
            g.Children.Add(info);
            g.Children.Add(dist);
            var card = Panel(g);
            card.Padding = new Thickness(12);
            if (onClick != null) OnClick(card, onClick);
            return card;
        }

        public static Border VehicleCard(Vehicle v, Action? onClick)
        {
            var (label, tone) = DemoConfig.VehicleStatusLabel(v.Status);
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            var tile = IconTile(Icons.Bike, Tone.Info, 52);
            tile.Background = Theme.SkySubtle;
            tile.Margin = new Thickness(0, 0, 12, 0);
            var bar = BatteryBar(v.Battery);
            bar.Width = 120;
            bar.VerticalAlignment = VerticalAlignment.Center;
            var info = Stack(6,
                Split(Text(v.Code, TextKind.Subtitle), Badge(label, tone)),
                Text($"{v.TypeLabel} · Slot {v.Slot}", TextKind.Caption, Theme.Text2),
                HStack(8, Icon(Icons.Bolt, 14, Theme.Success), bar, Text($"{v.Battery}% · ~{v.RangeKm} km", TextKind.Caption, null, false)));
            Grid.SetColumn(info, 1);
            g.Children.Add(tile);
            g.Children.Add(info);
            var card = Panel(g);
            card.Padding = new Thickness(12);
            if (v.Status is VehicleStatus.Faulty or VehicleStatus.UnderCheck) card.Opacity = 0.6;
            if (onClick != null) OnClick(card, onClick);
            return card;
        }

        public static Border StatTile(string glyph, string value, string label, Tone tone = Tone.Info) =>
            Panel(Stack(8, IconTile(glyph, tone, 28), Text(value, TextKind.Heading, null, false), Text(label, TextKind.Caption, Theme.Text2)));

        public static StackPanel EmptyState(string glyph, string title, string body)
        {
            var tile = IconTile(glyph, Tone.Neutral, 64);
            tile.HorizontalAlignment = HorizontalAlignment.Center;
            var sp = Stack(12, tile, Centered(Text(title, TextKind.Heading)), Centered(Text(body, TextKind.Body, Theme.Text2)));
            sp.Margin = new Thickness(0, 40, 0, 0);
            return sp;
        }

        public static Border MapPreview(IEnumerable<Hub> hubs, Action<Hub>? onHub)
        {
            var canvas = new Canvas { Height = 184, ClipToBounds = true };
            void Shape(System.Windows.Shapes.Shape r, double x, double y)
            {
                Canvas.SetLeft(r, x);
                Canvas.SetTop(r, y);
                canvas.Children.Add(r);
            }
            // Khu cây xanh / hồ (trang trí, không phải bản đồ thật).
            Shape(new System.Windows.Shapes.Ellipse { Width = 120, Height = 70, Fill = Theme.Park, Opacity = 0.7 }, -20, 100);
            Shape(new System.Windows.Shapes.Ellipse { Width = 90, Height = 60, Fill = Theme.Park, Opacity = 0.6 }, 290, 8);
            Shape(new System.Windows.Shapes.Ellipse { Width = 70, Height = 40, Fill = Theme.Sky, Opacity = 0.18 }, 190, 120);
            void Road(double x, double y, double w, double h) =>
                Shape(new System.Windows.Shapes.Rectangle { Width = w, Height = h, Fill = Theme.Surface, RadiusX = 4, RadiusY = 4, Opacity = 0.95 }, x, y);
            Road(0, 70, 400, 14);
            Road(150, 0, 14, 184);
            Road(40, 132, 360, 10);
            Road(260, 0, 10, 184);
            // Tuyến Metro số 1 (nét đứt).
            var metro = new System.Windows.Shapes.Line { X1 = 0, Y1 = 30, X2 = 400, Y2 = 52, Stroke = Theme.Charging, StrokeThickness = 3, StrokeDashArray = new DoubleCollection { 3, 2 }, Opacity = 0.55 };
            canvas.Children.Add(metro);
            foreach (var h in hubs)
            {
                var hub = h;
                var free = hub.IsSuspended ? Theme.Text2 : hub.FreeSlots == 0 ? Theme.Danger : Theme.Primary;
                var pin = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Background = free,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2.5),
                    Child = Icon(Icons.Pin, 13, Brushes.White),
                    ToolTip = hub.Name,
                    Effect = Theme.SoftShadow(8, 2, 0.25)
                };
                if (onHub != null) OnClick(pin, () => onHub(hub));
                // Toạ độ km so với vị trí sinh viên (CampusMap); tỉ lệ chọn để 12 Hub của khu ĐHQG nằm gọn trong khung.
                Canvas.SetLeft(pin, Math.Clamp(157 + hub.X * 160 - 15, 4, 320));
                Canvas.SetTop(pin, Math.Clamp(77 - hub.Y * 80 - 15, 4, 148));
                canvas.Children.Add(pin);
            }
            var halo = new System.Windows.Shapes.Ellipse { Width = 34, Height = 34, Fill = Theme.Primary, Opacity = 0.18 };
            Shape(halo, 140, 60);
            var me = new System.Windows.Shapes.Ellipse { Width = 16, Height = 16, Fill = Theme.Primary, Stroke = Brushes.White, StrokeThickness = 3 };
            Shape(me, 149, 69);
            var you = new Border { Background = Theme.Surface, CornerRadius = new CornerRadius(12), Padding = new Thickness(10, 4, 10, 4), Effect = Theme.SoftShadow(8, 1, 0.12), Child = HStack(6, Icon(Icons.Pin, 12, Theme.Primary), Text("Vị trí của bạn", TextKind.Caption, null, false)) };
            Canvas.SetLeft(you, 12);
            Canvas.SetTop(you, 148);
            canvas.Children.Add(you);
            return new Border { CornerRadius = new CornerRadius(20), Background = Theme.MapGradient, Child = canvas, ClipToBounds = true, BorderBrush = Theme.IsDark ? Theme.Border : Theme.SurfaceAlt, BorderThickness = new Thickness(1) };
        }
    }
}
