using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Ui
{
    /// <summary>Bộ dựng giao diện của app Sinh viên — phần Bề mặt: Panel, Hero, Card, Row, Section, Banner.</summary>
    public static partial class UiKit
    {
        public static Border Panel(UIElement child, Brush? background = null) => new()
        {
            Background = background ?? Theme.Surface,
            BorderBrush = Theme.IsDark ? Theme.Border : Theme.SurfaceAlt,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(16),
            Child = child,
            Effect = Theme.SoftShadow()
        };

        /// <summary>Thẻ nền gradient (tiêu đề Trang chủ, đồng hồ chuyến đi…).</summary>
        public static Border Hero(UIElement child) => new()
        {
            Background = Theme.HeroGradient,
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(18),
            Child = child,
            Effect = Theme.SoftShadow(26, 6, 0.28, "#4F46E5")
        };

        /// <summary>Hiệu ứng rê chuột: hơi mờ khi hover, đậm lại khi rời (WPF không có hover mặc định cho Border).</summary>
        public static T Hover<T>(T element) where T : UIElement
        {
            var baseOpacity = element.Opacity;
            element.MouseEnter += (_, _) => element.Opacity = baseOpacity * 0.88;
            element.MouseLeave += (_, _) => element.Opacity = baseOpacity;
            return element;
        }

        public static Border Card(params UIElement?[] children) => Panel(Stack(12, children));

        public static Grid Row(string label, string value, Brush? valueColor = null)
        {
            var value1 = Text(value, TextKind.Subtitle, valueColor, false);
            value1.TextAlignment = TextAlignment.Right;
            return Split(Text(label, TextKind.Body, Theme.Text2), value1);
        }

        public static Border Divider() => new() { Height = 1, Background = Theme.Border };

        public static TextBlock Section(string title) => Text(title, TextKind.Subtitle);

        public static Grid SectionWithLink(string title, string link, Action onClick)
        {
            var l = Text(link, TextKind.Subtitle, Theme.Primary, false);
            OnClick(l, onClick);
            return Split(Section(title), l);
        }

        public static StackPanel Labeled(string label, UIElement content) => Stack(8, Text(label, TextKind.Subtitle), content);

        public static Border Banner(Tone tone, string message)
        {
            var (bg, fg) = Theme.ToneBrushes(tone);
            var glyph = tone switch
            {
                Tone.Success => Icons.Check,
                Tone.Danger => Icons.Warning,
                Tone.Warning => Icons.Warning,
                Tone.Charging => Icons.Bolt,
                _ => Icons.Info
            };
            var icon = Icon(glyph, 16, fg);
            icon.VerticalAlignment = VerticalAlignment.Top;
            icon.Margin = new Thickness(0, 2, 10, 0);
            var dp = new DockPanel();
            DockPanel.SetDock(icon, Dock.Left);
            dp.Children.Add(icon);
            dp.Children.Add(Text(message));
            return new Border { Background = bg, CornerRadius = new CornerRadius(12), Padding = new Thickness(12), Child = dp };
        }
    }
}
