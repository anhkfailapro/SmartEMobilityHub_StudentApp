using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ReserveParkingScreen : Screen
    {
        private static readonly int[] Durations = { 60, 120, 180, 240 };
        private string _hubId = CampusMap.DefaultHubId;
        private string _vehicleId = "PV1";
        private bool _arriveNow;
        private int _day;
        private DateTime _arrival = TimeOptions.Slots(0)[0];
        private int _duration = 120;

        public override string? Title => "Đặt chỗ đỗ xe";

        public override UIElement BuildContent()
        {
            var store = DemoApp.Store;
            var root = Stack(16,
                Labeled("Hub", ChipGroup(store.Hubs, store.GetHub(_hubId), h => $"{h.ShortName} ({h.FreeSlots} chỗ)", h =>
                {
                    _hubId = h.Id;
                    Shell.Refresh();
                })),
                Labeled("Xe cá nhân", ChipGroup(store.PrivateVehicles, store.GetPrivateVehicle(_vehicleId), p => $"{p.Plate} · {p.Model}", p =>
                {
                    _vehicleId = p.Id;
                    Shell.Refresh();
                })),
                Panel(Toggle("Đến ngay (dùng giờ hiện tại)", _arriveNow, v =>
                {
                    _arriveNow = v;
                    Shell.Refresh();
                })));
            if (!_arriveNow)
            {
                Add(root, Labeled("Ngày", ChipGroup(Enumerable.Range(0, TimeOptions.Days.Length), _day, i => TimeOptions.Days[i], i =>
                {
                    _day = i;
                    _arrival = TimeOptions.Slots(i)[0];
                    Shell.Refresh();
                })), 16);
                Add(root, Labeled("Giờ đến", ChipGroup(TimeOptions.Slots(_day), _arrival, t => DemoConfig.Time(t), t =>
                {
                    _arrival = t;
                    Shell.Refresh();
                })), 16);
            }
            Add(root, Labeled("Thời lượng dự kiến", ChipGroup(Durations, _duration, d => ScreenHelpers.Duration(d), d =>
            {
                _duration = d;
                Shell.Refresh();
            })), 16);
            Add(root, Banner(Tone.Info, $"Mỗi tài khoản chỉ có {DemoConfig.MaxActiveParking} đặt chỗ đỗ xe đang hoạt động. Phí đỗ xe: Miễn phí (demo)."), 16);
            return root;
        }

        public override UIElement? BuildActions() => Button("Xác nhận đặt chỗ", ButtonVariant.Primary, () => Submit(_hubId, false));

        public void Submit(string hubId, bool alternativeHub)
        {
            var arrival = _arriveNow ? DemoApp.Clock.Now : _arrival;
            var result = DemoApp.Parking.ReserveParking(hubId, _vehicleId, arrival, _duration, alternativeHub);
            switch (result.Error)
            {
                case "":
                    Shell.BackTo<ReserveParkingScreen>();
                    Shell.Replace(new ParkingDetailScreen(result.Value!.Id, true));
                    break;
                case "Full":
                    Shell.Navigate(new ParkingFullScreen(this, hubId, result.Message));
                    break;
                case "LimitReached":
                    Shell.ShowDialog(Tone.Danger, "Không thể tạo đặt chỗ", result.Message,
                        new DialogAction("Xem đặt chỗ của tôi", ButtonVariant.Primary, () => DemoApp.GoTab(NavTab.Booking)),
                        new DialogAction("Đóng", ButtonVariant.Outline));
                    break;
                default:
                    Shell.ShowDialog(Tone.Warning, "Không thể tạo đặt chỗ", result.Message);
                    break;
            }
        }
    }
}
