using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class UnlockFailedScreen : Screen
    {
        private readonly VehicleReservation _reservation;
        private readonly Incident _incident;

        public UnlockFailedScreen(VehicleReservation reservation, Incident incident)
        {
            _reservation = reservation;
            _incident = incident;
        }

        public override string? Title => "Không mở khóa được";

        public override UIElement BuildContent() => Stack(16,
            Banner(Tone.Danger, $"Xe {_reservation.VehicleCode} không xác nhận mở khóa sau {DemoConfig.UnlockTimeoutSeconds} giây. Lệnh mở khóa đã được hủy."),
            Card(
                Row("Trạng thái xe", "Vẫn giữ cho bạn (Đã đặt)", Theme.Warning),
                Row("Giữ đến", DemoConfig.Time(_reservation.HoldUntil)),
                Row("Mã sự cố", _incident.Code),
                Row("Trạng thái sự cố", "Đã gửi người điều hành")),
            Card(
                Text("Liên hệ hỗ trợ", TextKind.Subtitle),
                Row("Hotline", DemoConfig.SupportHotline),
                Row("Giờ hỗ trợ", DemoConfig.SupportHours)));

        public override UIElement? BuildActions() => Columns(12,
            Button("Về đặt chỗ", ButtonVariant.Outline, () => Shell.BackTo<ReservationScreen>()),
            Button("Thử lại", ButtonVariant.Primary, () => Shell.Replace(new ScanQrScreen())));
    }
}
