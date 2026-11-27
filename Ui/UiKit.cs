using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Ui
{
    /// <summary>
    /// Bộ dựng giao diện của app Sinh viên, chia theo nhóm (DC-50): UiKit.cs (chữ, bố cục),
    /// .Surfaces, .Controls, .Inputs, .Indicators, .Domain. Màn hình dùng <c>using static UiKit</c>.
    /// </summary>
    public static partial class UiKit
    {
        public static readonly FontFamily Font = new("Roboto, Segoe UI");
        public static readonly FontFamily IconFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");

        public static TextBlock Text(string text, TextKind kind = TextKind.Body, Brush? color = null, bool wrap = true)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontFamily = Font,
                Foreground = color ?? Theme.Text,
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            switch (kind)
            {
                case TextKind.Title: tb.FontSize = 24; tb.FontWeight = FontWeights.Bold; break;
                case TextKind.Heading: tb.FontSize = 18; tb.FontWeight = FontWeights.Bold; break;
                case TextKind.Subtitle: tb.FontSize = 14; tb.FontWeight = FontWeights.Medium; break;
                case TextKind.Caption: tb.FontSize = 12; break;
                case TextKind.Button: tb.FontSize = 15; tb.FontWeight = FontWeights.Medium; break;
                case TextKind.Big: tb.FontSize = 30; tb.FontWeight = FontWeights.Bold; break;
                case TextKind.Huge: tb.FontSize = 46; tb.FontWeight = FontWeights.Bold; break;
                default: tb.FontSize = 14; break;
            }
            return tb;
        }

        public static TextBlock Centered(TextBlock tb)
        {
            tb.TextAlignment = TextAlignment.Center;
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            return tb;
        }

        public static TextBlock Icon(string glyph, double size = 18, Brush? color = null) => new()
        {
            Text = glyph,
            FontFamily = IconFont,
            FontSize = size,
            Foreground = color ?? Theme.Text,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        public static StackPanel Stack(double gap, params UIElement?[] children)
        {
            var sp = new StackPanel();
            foreach (var c in children) Add(sp, c, gap);
            return sp;
        }

        public static StackPanel HStack(double gap, params UIElement?[] children)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var c in children) Add(sp, c, gap);
            return sp;
        }

        public static void Add(StackPanel panel, UIElement? child, double gap)
        {
            if (child == null) return;
            if (panel.Children.Count > 0 && child is FrameworkElement fe)
            {
                var m = fe.Margin;
                fe.Margin = panel.Orientation == Orientation.Vertical
                    ? new Thickness(m.Left, m.Top + gap, m.Right, m.Bottom)
                    : new Thickness(m.Left + gap, m.Top, m.Right, m.Bottom);
            }
            panel.Children.Add(child);
        }

        public static UniformGrid Columns(double gap, params UIElement[] items)
        {
            var g = new UniformGrid { Rows = 1, Columns = Math.Max(1, items.Length) };
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] is FrameworkElement fe)
                    fe.Margin = new Thickness(i == 0 ? 0 : gap / 2, 0, i == items.Length - 1 ? 0 : gap / 2, 0);
                g.Children.Add(items[i]);
            }
            return g;
        }

        public static Grid Split(UIElement left, UIElement? right)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.Children.Add(left);
            if (right != null)
            {
                if (right is FrameworkElement fe) fe.Margin = new Thickness(12, 0, 0, 0);
                Grid.SetColumn(right, 1);
                g.Children.Add(right);
            }
            return g;
        }

        public static T OnClick<T>(T element, Action action) where T : FrameworkElement
        {
            element.Cursor = Cursors.Hand;
            if (element is Border b && b.Background == null) b.Background = Brushes.Transparent;
            element.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                action();
            };
            return element;
        }
    }
}
