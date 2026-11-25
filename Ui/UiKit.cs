using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Ui
{
    public static class UiKit
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

        public static Border Panel(UIElement child, Brush? background = null) => new()
        {
            Background = background ?? Theme.Surface,
            BorderBrush = Theme.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Child = child
        };

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

        public static Border Button(string label, ButtonVariant variant, Action? onClick, bool enabled = true, string? glyph = null)
        {
            Brush bg, fg, border;
            switch (variant)
            {
                case ButtonVariant.Primary: bg = Theme.Primary; fg = Theme.OnPrimary; border = Theme.Primary; break;
                case ButtonVariant.Secondary: bg = Theme.PrimarySubtle; fg = Theme.Primary; border = Theme.PrimarySubtle; break;
                case ButtonVariant.Danger: bg = Theme.Danger; fg = Theme.OnPrimary; border = Theme.Danger; break;
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
                CornerRadius = new CornerRadius(12),
                MinHeight = 48,
                Padding = new Thickness(12, 8, 12, 8),
                Child = content,
                Opacity = enabled ? 1 : 0.4
            };
            if (enabled && onClick != null) OnClick(b, onClick);
            return b;
        }

        public static Border Chip(string label, bool selected, Action? onClick)
        {
            var b = new Border
            {
                Background = selected ? Theme.Primary : Theme.Surface,
                BorderBrush = selected ? Theme.Primary : Theme.Border,
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
                    Child = Centered(Text(options[i], TextKind.Subtitle, on ? Theme.Primary : Theme.Text2))
                };
                OnClick(item, () => onSelect(index));
                g.Children.Add(item);
            }
            return new Border { Background = Theme.SurfaceAlt, CornerRadius = new CornerRadius(12), Padding = new Thickness(4), Child = g };
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
                CornerRadius = new CornerRadius(12),
                Background = bg,
                Child = Icon(glyph, size * 0.45, fg),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public static Border InputBox(string placeholder, string text, Action<string> onChange, bool multiline = false, Action? onEnter = null)
        {
            var tb = new TextBox
            {
                Text = text,
                FontFamily = Font,
                FontSize = 14,
                Background = Brushes.Transparent,
                Foreground = Theme.Text,
                CaretBrush = Theme.Text,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center
            };
            if (multiline)
            {
                tb.AcceptsReturn = true;
                tb.TextWrapping = TextWrapping.Wrap;
                tb.Height = 88;
            }
            var hint = Text(placeholder, TextKind.Body, Theme.Text2);
            hint.IsHitTestVisible = false;
            hint.VerticalAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center;
            hint.Margin = new Thickness(2, multiline ? 1 : 0, 0, 0);
            hint.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
            tb.TextChanged += (_, _) =>
            {
                hint.Visibility = string.IsNullOrEmpty(tb.Text) ? Visibility.Visible : Visibility.Collapsed;
                onChange(tb.Text);
            };
            if (onEnter != null && !multiline)
                tb.KeyDown += (_, e) =>
                {
                    if (e.Key != System.Windows.Input.Key.Enter) return;
                    e.Handled = true;
                    onEnter();
                };
            var grid = new Grid();
            grid.Children.Add(hint);
            grid.Children.Add(tb);
            return new Border
            {
                Background = Theme.Surface,
                BorderBrush = Theme.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Child = grid
            };
        }

        /// <summary>Ô nhập mật khẩu (ẩn ký tự), cùng kiểu dáng với <see cref="InputBox"/>.</summary>
        public static Border PasswordInput(string placeholder, string password, Action<string> onChange, Action? onEnter = null)
        {
            var pb = new PasswordBox
            {
                Password = password,
                FontFamily = Font,
                FontSize = 14,
                Background = Brushes.Transparent,
                Foreground = Theme.Text,
                CaretBrush = Theme.Text,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            var hint = Text(placeholder, TextKind.Body, Theme.Text2);
            hint.IsHitTestVisible = false;
            hint.VerticalAlignment = VerticalAlignment.Center;
            hint.Margin = new Thickness(2, 0, 0, 0);
            hint.Visibility = string.IsNullOrEmpty(password) ? Visibility.Visible : Visibility.Collapsed;
            pb.PasswordChanged += (_, _) =>
            {
                hint.Visibility = string.IsNullOrEmpty(pb.Password) ? Visibility.Visible : Visibility.Collapsed;
                onChange(pb.Password);
            };
            if (onEnter != null)
                pb.KeyDown += (_, e) =>
                {
                    if (e.Key != System.Windows.Input.Key.Enter) return;
                    e.Handled = true;
                    onEnter();
                };
            var grid = new Grid();
            grid.Children.Add(hint);
            grid.Children.Add(pb);
            return new Border
            {
                Background = Theme.Surface,
                BorderBrush = Theme.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Child = grid
            };
        }

        public static Grid Toggle(string label, bool value, Action<bool> onChange)
        {
            var knob = new System.Windows.Shapes.Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Margin = new Thickness(3),
                HorizontalAlignment = value ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            var track = new Border
            {
                Width = 44,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = value ? Theme.Primary : Theme.BorderStrong,
                Child = knob
            };
            OnClick(track, () => onChange(!value));
            return Split(Text(label, TextKind.Subtitle), track);
        }

        public static Grid Progress(double fraction, Brush fill, Brush track, double height = 8)
        {
            fraction = Math.Clamp(fraction, 0, 1);
            var g = new Grid { Height = height };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(fraction, 0.0001), GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(1 - fraction, 0.0001), GridUnitType.Star) });
            var bg = new Border { Background = track, CornerRadius = new CornerRadius(height / 2) };
            Grid.SetColumnSpan(bg, 2);
            var fg = new Border { Background = fill, CornerRadius = new CornerRadius(height / 2) };
            g.Children.Add(bg);
            g.Children.Add(fg);
            return g;
        }

        public static Grid BatteryBar(int percent)
        {
            var color = percent > 50 ? Theme.Success : percent >= DemoConfig.NeedsChargeBelow ? Theme.Warning : Theme.Danger;
            return Progress(percent / 100.0, color, Theme.SurfaceAlt);
        }

        public static Canvas Qr(string seedText, double size = 132)
        {
            var canvas = new Canvas { Width = size, Height = size, Background = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
            var cell = size / 11;
            var seed = seedText.Aggregate(7, (a, ch) => unchecked(a * 31 + ch)) & 0x7fffffff;
            var rnd = new Random(seed);
            for (var y = 0; y < 11; y++)
            {
                for (var x = 0; x < 11; x++)
                {
                    var finder = (x < 3 && y < 3) || (x > 7 && y < 3) || (x < 3 && y > 7);
                    if (!finder && rnd.NextDouble() <= 0.55) continue;
                    var r = new System.Windows.Shapes.Rectangle { Width = cell, Height = cell, Fill = Brushes.Black };
                    Canvas.SetLeft(r, x * cell);
                    Canvas.SetTop(r, y * cell);
                    canvas.Children.Add(r);
                }
            }
            return canvas;
        }

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
            var canvas = new Canvas { Height = 180, ClipToBounds = true };
            void Road(double x, double y, double w, double h)
            {
                var r = new System.Windows.Shapes.Rectangle { Width = w, Height = h, Fill = Theme.Surface, RadiusX = 3, RadiusY = 3 };
                Canvas.SetLeft(r, x);
                Canvas.SetTop(r, y);
                canvas.Children.Add(r);
            }
            Road(0, 70, 400, 14);
            Road(150, 0, 14, 180);
            Road(40, 130, 360, 10);
            Road(260, 0, 10, 180);
            foreach (var h in hubs)
            {
                var hub = h;
                var pin = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(14), Background = Theme.Surface, Child = Icon(Icons.Pin, 14, Theme.Primary), ToolTip = hub.Name };
                if (onHub != null) OnClick(pin, () => onHub(hub));
                Canvas.SetLeft(pin, Math.Clamp(157 + hub.X * 120 - 14, 4, 320));
                Canvas.SetTop(pin, Math.Clamp(77 - hub.Y * 60 - 14, 4, 148));
                canvas.Children.Add(pin);
            }
            var me = new System.Windows.Shapes.Ellipse { Width = 16, Height = 16, Fill = Theme.Primary, Stroke = Brushes.White, StrokeThickness = 3 };
            Canvas.SetLeft(me, 149);
            Canvas.SetTop(me, 69);
            canvas.Children.Add(me);
            var you = new Border { Background = Theme.Surface, CornerRadius = new CornerRadius(12), Padding = new Thickness(10, 4, 10, 4), Child = HStack(6, Icon(Icons.Pin, 12, Theme.Primary), Text("Vị trí của bạn", TextKind.Caption, null, false)) };
            Canvas.SetLeft(you, 12);
            Canvas.SetTop(you, 144);
            canvas.Children.Add(you);
            return new Border { CornerRadius = new CornerRadius(16), Background = Theme.SkySubtle, Child = canvas, ClipToBounds = true };
        }
    }
}
