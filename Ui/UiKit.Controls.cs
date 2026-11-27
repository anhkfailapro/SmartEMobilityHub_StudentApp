using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Ui
{
    /// <summary>Bộ dựng giao diện của app Sinh viên — phần Điều khiển: Button, Chip, Segmented, Badge, IconTile.</summary>
    public static partial class UiKit
    {
        public static Border Button(string label, ButtonVariant variant, Action? onClick, bool enabled = true, string? glyph = null)
        {
            Brush bg, fg, border;
            switch (variant)
            {
                case ButtonVariant.Primary: bg = Theme.PrimaryGradient; fg = Theme.OnPrimary; border = Brushes.Transparent; break;
                case ButtonVariant.Secondary: bg = Theme.PrimarySubtle; fg = Theme.Primary; border = Theme.PrimarySubtle; break;
                case ButtonVariant.Danger: bg = Theme.Danger; fg = Theme.OnPrimary; border = Brushes.Transparent; break;
                case ButtonVariant.DangerSubtle: bg = Theme.DangerSubtle; fg = Theme.Danger; border = Theme.DangerSubtle; break;
                case ButtonVariant.DangerOutline: bg = Theme.Surface; fg = Theme.Danger; border = Theme.Danger; break;
                case ButtonVariant.Charging: bg = Theme.ChargingSubtle; fg = Theme.Charging; border = Theme.ChargingSubtle; break;
                default: bg = Theme.Surface; fg = Theme.Text; border = Theme.BorderStrong; break;
            }
            var text = Text(label, TextKind.Button, fg);
            text.TextAlignment = TextAlignment.Center;
            UIElement content = text;
            if (glyph != null)
            {
                var hs = HStack(8, Icon(glyph, 16, fg), text);
                hs.HorizontalAlignment = HorizontalAlignment.Center;
                content = hs;
            }
            else
            {
                text.HorizontalAlignment = HorizontalAlignment.Center;
            }
            var b = new Border
            {
                Background = bg,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                MinHeight = 48,
                Padding = new Thickness(12, 8, 12, 8),
                Child = content,
                Opacity = enabled ? 1 : 0.4
            };
            if (enabled && variant is ButtonVariant.Primary or ButtonVariant.Danger)
                b.Effect = Theme.SoftShadow(16, 4, 0.22, variant == ButtonVariant.Primary ? "#2563EB" : "#DC2626");
            if (enabled && onClick != null)
            {
                OnClick(b, onClick);
                Hover(b);
            }
            return b;
        }

        public static Border Chip(string label, bool selected, Action? onClick)
        {
            var b = new Border
            {
                Background = selected ? Theme.PrimaryGradient : Theme.Surface,
                BorderBrush = selected ? Brushes.Transparent : Theme.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Child = Text(label, TextKind.Subtitle, selected ? Theme.OnPrimary : Theme.Text, false)
            };
            if (onClick != null) OnClick(b, onClick);
            return b;
        }

        public static WrapPanel ChipGroup<T>(IEnumerable<T> options, T selected, Func<T, string> label, Action<T> onSelect)
        {
            var w = new WrapPanel();
            foreach (var o in options)
            {
                var opt = o;
                w.Children.Add(Chip(label(opt), EqualityComparer<T>.Default.Equals(opt, selected), () => onSelect(opt)));
            }
            return w;
        }

        public static Border Segmented(string[] options, int selected, Action<int> onSelect)
        {
            var g = new UniformGrid { Rows = 1, Columns = options.Length };
            for (var i = 0; i < options.Length; i++)
            {
                var index = i;
                var on = i == selected;
                var item = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(6, 8, 6, 8),
                    Background = on ? Theme.Surface : Brushes.Transparent,
                    Child = Centered(Text(options[i], TextKind.Subtitle, on ? Theme.Primary : Theme.Text2)),
                    Effect = on ? Theme.SoftShadow(8, 1, 0.08) : null
                };
                OnClick(item, () => onSelect(index));
                g.Children.Add(item);
            }
            return new Border { Background = Theme.SurfaceAlt, CornerRadius = new CornerRadius(14), Padding = new Thickness(4), Child = g };
        }

        public static Border Badge(string label, Tone tone)
        {
            var (bg, fg) = Theme.ToneBrushes(tone);
            var dot = new System.Windows.Shapes.Ellipse { Width = 6, Height = 6, Fill = fg, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(dot);
            sp.Children.Add(Text(label, TextKind.Caption, fg, false));
            return new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 3, 10, 3),
                Child = sp,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public static Border IconTile(string glyph, Tone tone, double size = 46)
        {
            var (bg, fg) = Theme.ToneBrushes(tone);
            return new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size * 0.32),
                Background = bg,
                Child = Icon(glyph, size * 0.45, fg),
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }
}
