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

        public UnlockFailedScreen(DemoApp app, VehicleReservation reservation, Incident incident) : base(app)
        {
            _reservation = reservation;
            _incident = incident;
        }

        public override string? Title => "Không mở khóa được";

        // Theo TCOP03-05 (Core): lệnh mở khoá bị huỷ, đặt xe bị huỷ (không mất phí) và xe được giữ ở trạng thái
        // "Đã đặt" cho tới khi người vận hành kiểm tra xong sự cố. Trước DC-54 app Sinh viên vẫn giữ đặt xe và
        // cho "Thử lại", trong khi Trung tâm điều hành đã huỷ — hai bên lệch nhau.
        public override UIElement BuildContent() => Stack(16,
            Banner(Tone.Danger, $"Xe {_reservation.VehicleCode} không xác nhận mở khóa sau {DemoConfig.UnlockTimeoutSeconds} giây. Lệnh mở khóa đã được hủy."),
            Card(
                Row("Đặt xe", _reservation.State == ReservationState.Active ? "Vẫn đang giữ" : "Đã hủy · không mất phí", Theme.Warning),
                Row("Trạng thái xe", "Được giữ lại chờ kiểm tra", Theme.Warning),
                Row("Mã sự cố", _incident.Code),
                Row("Trạng thái sự cố", "Đã gửi người điều hành")),
            Card(
                Text("Liên hệ hỗ trợ", TextKind.Subtitle),
                Row("Hotline", DemoConfig.SupportHotline),
                Row("Giờ hỗ trợ", DemoConfig.SupportHours)));

        public override UIElement? BuildActions() => Columns(12,
            Button("Về Trang chủ", ButtonVariant.Outline, () => App.GoTab(NavTab.Home)),
            Button("Chọn xe khác", ButtonVariant.Primary, () => Shell.ResetTo(new VehicleListScreen(App, _reservation.HubId))));
    }
}
