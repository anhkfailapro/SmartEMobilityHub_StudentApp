using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ScheduleChargingScreen : Screen
    {
        public ScheduleChargingScreen(DemoApp app) : base(app)
        {
            _start = TimeOptions.Slots(App.Clock.Now, 0)[0];
        }

        private static readonly int[] Durations = { 60, 90, 120, 150, 180, 240 };
        private static readonly int[] Targets = { 80, 90, 100 };
        private string _hubId = CampusMap.DefaultHubId;
        private string _vehicleId = "PV2";
        private bool _asap;
        private int _day;
        private DateTime _start;
        private int _duration = 90;
        private int _target = 80;

        public override string? Title => "Lên lịch sạc";

        public int Duration => _duration;

        public override UIElement BuildContent()
        {
            var store = App.Store;
            var hub = store.GetHub(_hubId);
            var root = Stack(16,
                Labeled("Hub", ChipGroup(store.Hubs, hub, h => h.ShortName, h =>
                {
                    _hubId = h.Id;
                    Shell.Refresh();
                })),
                Text($"Giới hạn tại {hub.ShortName}: tối đa {hub.MaxChargingMinutes} phút mỗi lần · {hub.Points.Count(p => p.Status != PointStatus.OutOfService)} cổng hoạt động", TextKind.Caption, Theme.Text2),
                Labeled("Xe cá nhân", ChipGroup(store.PrivateVehicles, store.GetPrivateVehicle(_vehicleId), p => $"{p.Plate} · {p.Model}", p =>
                {
                    _vehicleId = p.Id;
                    Shell.Refresh();
                })),
                Panel(Toggle("Sạc sớm nhất có thể", _asap, v =>
                {
                    _asap = v;
                    Shell.Refresh();
                })));
            if (_asap)
            {
                var earliest = App.Booking.FindEarliestTime(_hubId);
                Add(root, Banner(Tone.Charging, $"Khung trống sớm nhất tại {hub.ShortName}: {DemoConfig.Time(earliest)}–{DemoConfig.Time(earliest.AddMinutes(_duration))} hôm nay."), 16);
            }
            else
            {
                Add(root, Labeled("Ngày", ChipGroup(Enumerable.Range(0, TimeOptions.Days.Length), _day, i => TimeOptions.Days[i], i =>
                {
                    _day = i;
                    _start = TimeOptions.Slots(App.Clock.Now, i)[0];
                    Shell.Refresh();
                })), 16);
                Add(root, Labeled("Giờ bắt đầu", ChipGroup(TimeOptions.Slots(App.Clock.Now, _day), _start, t => DemoConfig.Time(t), t =>
                {
                    _start = t;
                    Shell.Refresh();
                })), 16);
            }
            Add(root, Labeled("Thời lượng dự kiến", ChipGroup(Durations, _duration, d => $"{d} phút", d =>
            {
                _duration = d;
                Shell.Refresh();
            })), 16);
            Add(root, Labeled("Mức pin mục tiêu", ChipGroup(Targets, _target, t => $"{t}%", t =>
            {
                _target = t;
                Shell.Refresh();
            })), 16);
            Add(root, Card(
                Row("Chi phí ước tính", $"~{DemoConfig.Money(App.Booking.EstimateCost(_duration))}"),
                Row("Nhắc lịch", $"Trước {DemoConfig.ReminderMinutesBefore} phút"),
                Row("Chỗ đỗ", "Tự động giữ cùng khung giờ")), 16);
            return root;
        }

        public override UIElement? BuildActions() => Button("Xác nhận lịch sạc", ButtonVariant.Primary, () =>
        {
            if (_asap)
            {
                var earliest = App.Booking.FindEarliestTime(_hubId);
                Shell.ShowDialog(Tone.Charging, "Khung trống sớm nhất",
                    $"{App.Store.GetHub(_hubId).Name}: {DemoConfig.Time(earliest)}–{DemoConfig.Time(earliest.AddMinutes(_duration))}. Đặt lịch sạc vào khung này?",
                    new DialogAction("Đồng ý", ButtonVariant.Primary, () => Submit(_hubId, earliest, false)),
                    new DialogAction("Chọn giờ khác", ButtonVariant.Outline, () =>
                    {
                        _asap = false;
                        Shell.Refresh();
                    }));
                return;
            }
            Submit(_hubId, _start, false);
        });

        public void Submit(string hubId, DateTime start, bool alternative)
        {
            var durationError = App.Booking.ValidateDuration(hubId, _duration);
            if (durationError != null)
            {
                Shell.ShowDialog(Tone.Warning, "Thời lượng vượt giới hạn", durationError,
                    new DialogAction("Sửa thời lượng", ButtonVariant.Primary, () => Shell.BackTo<ScheduleChargingScreen>()));
                return;
            }
            var result = App.Booking.ScheduleCharging(hubId, _vehicleId, start, _duration, _target, alternative);
            switch (result.Error)
            {
                case "":
                    Shell.BackTo<ScheduleChargingScreen>();
                    Shell.Replace(new ChargingBookingDetailScreen(App, result.Value!.Id, true));
                    break;
                case "NoPoint":
                    Shell.Navigate(new ChargingAlternativesScreen(App, this, hubId, start, result.Message));
                    break;
                case "LimitReached":
                    Shell.ShowDialog(Tone.Danger, "Không thể đặt lịch sạc", result.Message,
                        new DialogAction("Xem đặt chỗ của tôi", ButtonVariant.Primary, () => App.GoTab(NavTab.Booking)),
                        new DialogAction("Đóng", ButtonVariant.Outline));
                    break;
                default:
                    Shell.ShowDialog(Tone.Warning, "Không thể đặt lịch sạc", result.Message);
                    break;
            }
        }
    }
}
