using System.Windows;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class MyReservationsScreen : Screen
    {
        private bool _finished;

        public override string? Title => "Đặt chỗ của tôi";
        public override bool ShowBack => false;
        public override NavTab? Tab => NavTab.Booking;
        public override bool WantsTick => !_finished;

        public override UIElement BuildContent()
        {
            var active = DemoApp.Reservations.ListActive();
            var root = Stack(16, Segmented(new[] { $"Đang hoạt động ({active.Count})", "Đã kết thúc" }, _finished ? 1 : 0, i =>
            {
                _finished = i == 1;
                Shell.Refresh();
            }));
            Add(root, Columns(12,
                Button("Đặt chỗ đỗ xe", ButtonVariant.Secondary, () => Shell.Navigate(new ReserveParkingScreen()), true, Icons.Parking),
                Button("Lên lịch sạc", ButtonVariant.Charging, () => Shell.Navigate(new ScheduleChargingScreen()), true, Icons.Bolt)), 16);

            var items = _finished ? DemoApp.Reservations.ListFinished() : active;
            if (items.Count == 0)
            {
                Add(root, EmptyState(Icons.Calendar, _finished ? "Chưa có đặt chỗ đã kết thúc" : "Chưa có đặt chỗ nào", "Đặt xe, đặt chỗ đỗ xe hoặc lên lịch sạc để bắt đầu."), 16);
                return root;
            }
            foreach (var item in items) Add(root, ItemCard(item), 16);
            if (!_finished)
                Add(root, Banner(Tone.Danger, "Hủy trong vòng 5 phút trước giờ bắt đầu sẽ bị tính phí hủy muộn. Không thể hủy khi đã nhận xe."), 16);
            return root;
        }

        private UIElement ItemCard(BookingItem item)
        {
            var card = Card(Split(Text(item.Title, TextKind.Subtitle), Badge(item.StatusLabel, item.StatusTone)));
            var stack = (System.Windows.Controls.StackPanel)card.Child!;
            foreach (var (label, value) in item.Rows) Add(stack, Row(label, value), 12);
            if (!item.IsActive) return card;

            UIElement buttons = item.Kind switch
            {
                BookingKind.Vehicle when item.Started => Button("Hủy", ButtonVariant.DangerOutline, () => Cancel(item)),
                BookingKind.Vehicle => Columns(12,
                    Button("Xem mã nhận xe", ButtonVariant.Outline, () => Shell.Navigate(new ReservationScreen())),
                    Button("Hủy", ButtonVariant.DangerOutline, () => Cancel(item))),
                BookingKind.ChargingRequest => Button("Hủy yêu cầu", ButtonVariant.DangerOutline, () => Cancel(item)),
                _ => Columns(12,
                    Button("Đổi giờ", ButtonVariant.Outline, () => Shell.Navigate(new ChangeTimeScreen(item.Kind, item.Id)), item.CanChangeTime),
                    Button("Hủy", ButtonVariant.DangerOutline, () => Cancel(item)))
            };
            Add(stack, buttons, 12);
            return card;
        }

        private static void Cancel(BookingItem item)
        {
            var preview = DemoApp.Reservations.PreviewCancel(item.Kind, item.Id);
            if (!preview.Ok)
            {
                if (item.Kind == BookingKind.Vehicle)
                    Shell.ShowDialog(Tone.Danger, "Không thể hủy", preview.Message,
                        new DialogAction("Đến màn hình chuyến đi", ButtonVariant.Primary, () => DemoApp.GoTab(NavTab.Trip)),
                        new DialogAction("Đóng", ButtonVariant.Outline));
                else
                    Shell.ShowDialog(Tone.Danger, "Không thể hủy", preview.Message);
                return;
            }
            Shell.Navigate(new CancelConfirmScreen(item.Kind, item.Id));
        }
    }
}
