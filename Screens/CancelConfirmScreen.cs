using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class CancelConfirmScreen : Screen
    {
        private readonly BookingKind _kind;
        private readonly string _id;

        public CancelConfirmScreen(BookingKind kind, string id)
        {
            _kind = kind;
            _id = id;
        }

        public override string? Title => "Hủy đặt chỗ";

        private static string KindLabel(BookingKind k) => k switch
        {
            BookingKind.Vehicle => "Đặt xe chia sẻ",
            BookingKind.Parking => "Đỗ xe",
            BookingKind.ChargingSession => "Lịch sạc",
            _ => "Yêu cầu sạc"
        };

        public override UIElement BuildContent()
        {
            var item = DemoApp.Reservations.FindActive(_kind, _id);
            if (item == null) return EmptyState(Icons.Calendar, "Đặt chỗ không còn hoạt động", "Đặt chỗ có thể đã hết hạn hoặc đã bị hủy.");
            var preview = DemoApp.Reservations.PreviewCancel(_kind, _id);
            if (!preview.Ok) return Stack(16, Banner(Tone.Danger, preview.Message));

            var fee = preview.Value;
            var card = Card(Row("Loại", KindLabel(_kind)), Row("Đặt chỗ", item.Title));
            var stack = (System.Windows.Controls.StackPanel)card.Child!;
            foreach (var (label, value) in item.Rows) Add(stack, Row(label, value), 12);
            Add(stack, Row("Phí hủy", DemoConfig.Money(fee), fee > 0 ? Theme.Danger : Theme.Success), 12);

            string rule = _kind switch
            {
                BookingKind.Vehicle => "Xe sẽ được trả về trạng thái sẵn sàng. Hủy đặt xe không mất phí.",
                BookingKind.ChargingRequest => "Yêu cầu sẽ được xóa khỏi hàng chờ sạc của Hub.",
                _ => $"Quy định: hủy miễn phí khi còn hơn {DemoConfig.LateCancelWindowMinutes} phút trước giờ bắt đầu. Tài nguyên sẽ được trả lại cho Hub."
            };
            return Stack(16, card,
                fee > 0
                    ? Banner(Tone.Danger, $"Bạn hủy trong vòng {DemoConfig.LateCancelWindowMinutes} phút trước giờ bắt đầu nên bị tính phí hủy muộn {DemoConfig.Money(fee)}.")
                    : Banner(Tone.Info, rule));
        }

        public override UIElement? BuildActions()
        {
            var item = DemoApp.Reservations.FindActive(_kind, _id);
            if (item == null) return Button("Về Đặt chỗ của tôi", ButtonVariant.Primary, () => DemoApp.GoTab(NavTab.Booking));
            var preview = DemoApp.Reservations.PreviewCancel(_kind, _id);
            if (!preview.Ok) return Button("Đến màn hình chuyến đi", ButtonVariant.Primary, () => DemoApp.GoTab(NavTab.Trip));
            return Columns(12,
                Button("Quay lại", ButtonVariant.Outline, Shell.Back),
                Button(preview.Value > 0 ? "Hủy và trả phí" : "Xác nhận hủy", ButtonVariant.Danger, Confirm));
        }

        private void Confirm()
        {
            var fee = DemoApp.Reservations.Cancel(_kind, _id);
            DemoApp.GoTab(NavTab.Booking);
            Shell.ShowDialog(Tone.Success, "Đã hủy đặt chỗ",
                fee > 0
                    ? $"Đặt chỗ đã được hủy. Phí hủy muộn {DemoConfig.Money(fee)} đã được ghi nhận. Thông báo xác nhận đã được gửi."
                    : "Đặt chỗ đã được hủy và tài nguyên đã được trả lại. Thông báo xác nhận đã được gửi.");
        }
    }
}
