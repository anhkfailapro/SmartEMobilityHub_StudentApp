using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ConfirmReservationScreen : Screen
    {
        private readonly string _code;

        public ConfirmReservationScreen(string code)
        {
            _code = code;
        }

        public override string? Title => "Xác nhận đặt xe";

        public override UIElement BuildContent()
        {
            var v = DemoApp.Vehicles.GetDetails(_code)!;
            var hub = DemoApp.Store.GetHub(v.HubId ?? CampusMap.DefaultHubId);
            return Stack(16,
                Card(
                    ScreenHelpers.VehicleHeader(v, $"{v.TypeLabel} · Pin {v.Battery}% · ~{v.RangeKm} km"),
                    Divider(),
                    Row("Mã xe", v.Code),
                    Row("Hub", hub.ShortName),
                    Row("Vị trí đỗ", $"Slot {v.Slot}"),
                    Row("Thời gian giữ xe", $"{DemoConfig.HoldMinutes} phút"),
                    Divider(),
                    Row("Phí giữ xe", "Miễn phí", Theme.Success),
                    Row("Giá thuê", $"{DemoConfig.Money(DemoConfig.RentalPricePerMinute)} / phút")),
                Banner(Tone.Info, $"Xe sẽ được giữ cho bạn trong {DemoConfig.HoldMinutes} phút. Nếu quá thời gian mà chưa nhận xe, đặt chỗ sẽ tự động hủy."),
                Banner(Tone.Info, "Mỗi tài khoản chỉ có 1 đặt xe đang hoạt động tại một thời điểm."));
        }

        public override UIElement? BuildActions() => Columns(12,
            Button("Hủy", ButtonVariant.Outline, Shell.Back),
            Button("Xác nhận đặt", ButtonVariant.Primary, Confirm));

        private void Confirm()
        {
            var result = DemoApp.Reservations.ReserveVehicle(_code);
            if (!result.Ok)
            {
                ReservationDialogs.ShowReserveError(result.Error, _code);
                return;
            }
            Shell.Replace(new ReservationScreen(true));
        }
    }
}
