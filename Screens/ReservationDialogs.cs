using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;

namespace SmartEMobilityHub.Screens
{
    public static class ReservationDialogs
    {
        public static void ShowReserveError(string error, string vehicleCode)
        {
            var shell = DemoApp.Shell;
            switch (error)
            {
                case "AlreadyActive":
                    {
                        var r = DemoApp.Reservations.ActiveVehicleReservation;
                        var where = r == null ? "" : $" Đặt chỗ hiện tại: {r.VehicleCode} · {DemoApp.Store.GetHub(r.HubId).ShortName}.";
                        shell.ShowDialog(Tone.Info, "Bạn đã có 1 đặt xe đang hoạt động", "Mỗi tài khoản chỉ có 1 đặt xe tại một thời điểm." + where,
                            new DialogAction("Mở đặt chỗ hiện tại", ButtonVariant.Primary, () => shell.Navigate(new ReservationScreen())),
                            new DialogAction("Đóng", ButtonVariant.Outline));
                        break;
                    }
                case "TripInProgress":
                    shell.ShowDialog(Tone.Info, "Bạn đang có chuyến đi", "Hãy trả xe hiện tại trước khi đặt xe mới.",
                        new DialogAction("Đến chuyến đi", ButtonVariant.Primary, () => DemoApp.GoTab(NavTab.Trip)),
                        new DialogAction("Đóng", ButtonVariant.Outline));
                    break;
                case "HubSuspended":
                    {
                        var hubId = DemoApp.Store.GetVehicle(vehicleCode)?.HubId;
                        var name = hubId == null ? "Hub này" : DemoApp.Store.GetHub(hubId).Name;
                        shell.ShowDialog(Tone.Danger, "Hub đang tạm dừng hoạt động", $"{name} tạm dừng theo quyết định của người vận hành nên không nhận đặt xe. Vui lòng chọn Hub khác.",
                            new DialogAction("Về trang chủ", ButtonVariant.Primary, () => DemoApp.GoTab(NavTab.Home)),
                            new DialogAction("Đóng", ButtonVariant.Outline));
                        break;
                    }
                default:
                    shell.ShowDialog(Tone.Warning, "Xe vừa được người khác đặt", $"{vehicleCode} đã được một sinh viên khác đặt trước. Vui lòng chọn xe khác.",
                        new DialogAction("Về danh sách xe", ButtonVariant.Primary, () => shell.BackTo<VehicleListScreen>()));
                    break;
            }
        }
    }
}
