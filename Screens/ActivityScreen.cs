using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ActivityScreen : Screen
    {
        private readonly bool _fromHome;
        private int _tab;

        public ActivityScreen(bool fromHome)
        {
            _fromHome = fromHome;
        }

        public override string? Title => "Hoạt động";
        public override bool ShowBack => _fromHome;
        public override NavTab? Tab => NavTab.Notify;

        public override UIElement BuildContent()
        {
            var root = Stack(16, Segmented(new[] { "Thông báo", "Lịch sử" }, _tab, i =>
            {
                _tab = i;
                Shell.Refresh();
            }));
            Add(root, _tab == 0 ? BuildNotifications() : BuildHistory(), 16);
            return root;
        }

        private static UIElement BuildNotifications()
        {
            var list = DemoApp.Notifications.Newest();
            if (list.Count == 0) return EmptyState(Icons.Bell, "Chưa có thông báo", "Thông báo về đặt chỗ, thanh toán và sạc sẽ hiển thị ở đây.");
            var now = DemoApp.Clock.Now;
            var stack = Stack(12);
            foreach (var n in list)
            {
                var glyph = n.Tone switch
                {
                    Tone.Success => Icons.Check,
                    Tone.Danger => Icons.Close,
                    Tone.Warning => Icons.Warning,
                    Tone.Charging => Icons.Bolt,
                    _ => Icons.Receipt
                };
                var tile = IconTile(glyph, n.Tone, 36);
                tile.VerticalAlignment = VerticalAlignment.Top;
                var title = Text(n.Read ? n.Title : "● " + n.Title, TextKind.Subtitle, n.Read ? null : Theme.Primary);
                var body = Stack(4, Split(title, Text(DemoConfig.Ago(n.Time, now), TextKind.Caption, Theme.Text2, false)), Text(n.Body, TextKind.Body, Theme.Text2));
                Add(stack, Panel(HStackFill(tile, body)), 12);
            }
            DemoApp.Notifications.MarkAllRead();
            return stack;
        }

        private static UIElement HStackFill(UIElement left, UIElement right)
        {
            var g = new System.Windows.Controls.Grid();
            g.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
            if (left is FrameworkElement fe) fe.Margin = new Thickness(0, 0, 12, 0);
            System.Windows.Controls.Grid.SetColumn(right, 1);
            g.Children.Add(left);
            g.Children.Add(right);
            return g;
        }

        private static UIElement BuildHistory()
        {
            var entries = DemoApp.History.Entries();
            if (entries.Count == 0)
                return EmptyState(Icons.Clock, $"Không có hoạt động trong {DemoConfig.HistoryMonths} tháng gần đây", "Các chuyến đi và đặt chỗ của bạn sẽ hiển thị ở đây.");
            var stack = Stack(12, Text($"Chuyến đi và đặt chỗ trong {DemoConfig.HistoryMonths} tháng gần đây", TextKind.Caption, Theme.Text2));
            foreach (var e in entries)
            {
                var entry = e;
                var card = Card(
                    Split(Text(entry.Title, TextKind.Subtitle), Text(entry.Fee, TextKind.Subtitle, null, false)),
                    Text($"{entry.Date:dd/MM/yyyy HH:mm} · {entry.HubName}", TextKind.Caption, Theme.Text2),
                    Text($"{entry.Duration} · {entry.Status}", TextKind.Caption, Theme.Text2));
                Add(stack, OnClick(card, () => Shell.Navigate(new HistoryDetailScreen(entry))), 12);
            }
            return stack;
        }
    }
}
