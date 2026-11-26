using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Screens;

namespace SmartEMobilityHub.Ui
{
    public class AppShell
    {
        private readonly Border _host;
        private readonly List<Screen> _stack = new();
        private ScrollViewer? _scroll;
        private Grid? _root;
        private UIElement? _dialog;

        public AppShell(Border host)
        {
            _host = host;
        }

        public Screen? Current => _stack.Count > 0 ? _stack[^1] : null;

        public void Navigate(Screen screen)
        {
            DropDialog();
            _stack.Add(screen);
            Render(true);
        }

        public void Replace(Screen screen)
        {
            DropDialog();
            if (_stack.Count > 0) _stack.RemoveAt(_stack.Count - 1);
            _stack.Add(screen);
            Render(true);
        }

        public void Back()
        {
            DropDialog();
            if (_stack.Count > 1) _stack.RemoveAt(_stack.Count - 1);
            Render(true);
        }

        public void BackTo<T>() where T : Screen
        {
            DropDialog();
            while (_stack.Count > 1 && _stack[^1] is not T) _stack.RemoveAt(_stack.Count - 1);
            Render(true);
        }

        public void ResetTo(Screen screen)
        {
            DropDialog();
            _stack.Clear();
            _stack.Add(screen);
            Render(true);
        }

        public void Refresh() => Render(false);

        public void ShowDialog(Tone tone, string title, string body, params DialogAction[] actions)
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
            var icon = new Border { Width = 44, Height = 44, CornerRadius = new CornerRadius(22), Background = bg, Child = UiKit.Icon(glyph, 20, fg), HorizontalAlignment = HorizontalAlignment.Left };
            var stack = UiKit.Stack(12, icon, UiKit.Text(title, TextKind.Heading), UiKit.Text(body, TextKind.Body, Theme.Text2));
            var list = actions.Length == 0 ? new[] { new DialogAction("Đóng", ButtonVariant.Outline) } : actions;
            foreach (var a in list)
            {
                var act = a;
                UiKit.Add(stack, UiKit.Button(act.Label, act.Variant, () =>
                {
                    CloseDialog();
                    act.OnClick?.Invoke();
                }), 8);
            }
            var card = new Border { Background = Theme.Surface, CornerRadius = new CornerRadius(24), Padding = new Thickness(22), Margin = new Thickness(22), VerticalAlignment = VerticalAlignment.Center, Child = stack, Effect = Theme.SoftShadow(40, 10, 0.25) };
            var overlay = new Grid { Background = new SolidColorBrush(Color.FromArgb(130, 15, 23, 42)) };
            overlay.Children.Add(card);
            DropDialog();
            _dialog = overlay;
            Render(false);
        }

        public void CloseDialog()
        {
            DropDialog();
            Render(false);
        }

        private void DropDialog()
        {
            if (_root != null && _dialog != null) _root.Children.Remove(_dialog);
            _dialog = null;
        }

        private void Render(bool resetScroll)
        {
            var screen = Current;
            if (screen == null) return;
            if (_root != null && _dialog != null) _root.Children.Remove(_dialog);
            var offset = resetScroll || _scroll == null ? 0 : _scroll.VerticalOffset;

            var root = new Grid { Background = Theme.CanvasBg };
            for (var i = 0; i < 5; i++) root.RowDefinitions.Add(new RowDefinition { Height = i == 2 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

            Place(root, BuildStatusBar(), 0);
            if (screen.Title != null) Place(root, BuildAppBar(screen), 1);

            var content = screen.BuildContent();
            if (content is FrameworkElement fe) fe.Margin = new Thickness(16, 16, 16, 24);
            _scroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            Place(root, _scroll, 2);

            var actions = screen.BuildActions();
            if (actions != null)
            {
                Place(root, new Border
                {
                    Background = Theme.Surface,
                    BorderBrush = Theme.IsDark ? Theme.Border : Theme.SurfaceAlt,
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    CornerRadius = new CornerRadius(22, 22, 0, 0),
                    Padding = new Thickness(16, 14, 16, 16),
                    Child = actions,
                    Effect = Theme.SoftShadow(20, -2, 0.06)
                }, 3);
            }
            if (screen.Tab.HasValue) Place(root, BuildBottomNav(screen.Tab.Value), 4);

            if (_dialog != null)
            {
                Grid.SetRowSpan(_dialog, 5);
                root.Children.Add(_dialog);
            }
            _root = root;
            _host.Child = root;

            if (offset > 0)
            {
                var sv = _scroll;
                sv.Loaded += (_, _) => sv.ScrollToVerticalOffset(offset);
            }
        }

        private static void Place(Grid grid, UIElement element, int row)
        {
            Grid.SetRow(element, row);
            grid.Children.Add(element);
        }

        private static UIElement BuildStatusBar()
        {
            var time = UiKit.Text(DemoApp.Clock.Now.ToString("HH:mm"), TextKind.Subtitle, null, false);
            var right = UiKit.Text("▂▄▆  ▮▮▮", TextKind.Caption, null, false);
            var g = UiKit.Split(time, right);
            g.Height = 40;
            g.Margin = new Thickness(24, 0, 24, 0);
            return g;
        }

        private static UIElement BuildAppBar(Screen screen)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Height = 56, Margin = new Thickness(16, 0, 16, 0) };
            if (screen.ShowBack)
            {
                var back = new Border
                {
                    Width = 40,
                    Height = 40,
                    CornerRadius = new CornerRadius(14),
                    Background = Theme.Surface,
                    BorderBrush = Theme.Border,
                    BorderThickness = new Thickness(1),
                    Child = UiKit.Icon(Icons.Back, 15),
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                UiKit.OnClick(back, screen.OnBack);
                UiKit.Hover(back);
                sp.Children.Add(back);
            }
            sp.Children.Add(UiKit.Text(screen.Title ?? "", TextKind.Heading, null, false));
            return new Border { Background = Theme.CanvasBg, Child = sp };
        }

        private static UIElement BuildBottomNav(NavTab active)
        {
            var items = new (NavTab Tab, string Glyph, string Label)[]
            {
                (NavTab.Home, Icons.Home, "Trang chủ"),
                (NavTab.Booking, Icons.Calendar, "Đặt chỗ"),
                (NavTab.Trip, Icons.Bike, "Chuyến đi"),
                (NavTab.Notify, Icons.Bell, "Thông báo"),
                (NavTab.Account, Icons.Person, "Tài khoản")
            };
            var g = new UniformGrid { Rows = 1, Columns = items.Length, Height = 66 };
            foreach (var it in items)
            {
                var item = it;
                var on = item.Tab == active;
                var color = on ? Theme.Primary : Theme.Text2;
                var label = item.Label;
                var unread = item.Tab == NavTab.Notify ? DemoApp.Notifications.UnreadCount : 0;

                // Biểu tượng nằm trong "viên thuốc" khi tab đang chọn; thông báo chưa đọc có chấm đỏ.
                var iconHost = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
                iconHost.Children.Add(new Border
                {
                    Width = 54,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Background = on ? Theme.PrimarySubtle : Brushes.Transparent,
                    Child = UiKit.Icon(item.Glyph, 19, color)
                });
                if (unread > 0)
                {
                    iconHost.Children.Add(new Border
                    {
                        MinWidth = 16,
                        Height = 16,
                        CornerRadius = new CornerRadius(8),
                        Background = Theme.Danger,
                        BorderBrush = Theme.Surface,
                        BorderThickness = new Thickness(2),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, -3, 6, 0),
                        Padding = new Thickness(3, 0, 3, 0),
                        Child = UiKit.Centered(UiKit.Text(unread > 9 ? "9+" : unread.ToString(), TextKind.Caption, Brushes.White, false))
                    });
                    ((TextBlock)((Border)iconHost.Children[1]).Child).FontSize = 9;
                }
                var caption = UiKit.Centered(UiKit.Text(label, TextKind.Caption, color, false));
                caption.FontSize = 11;
                if (on) caption.FontWeight = FontWeights.SemiBold;
                var cell = new Border { Child = UiKit.Stack(3, iconHost, caption), Padding = new Thickness(0, 8, 0, 0) };
                UiKit.OnClick(cell, () => DemoApp.GoTab(item.Tab));
                g.Children.Add(cell);
            }
            return new Border
            {
                Background = Theme.Surface,
                BorderBrush = Theme.IsDark ? Theme.Border : Theme.SurfaceAlt,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Child = g,
                Effect = Theme.SoftShadow(18, -2, 0.06)
            };
        }
    }
}
