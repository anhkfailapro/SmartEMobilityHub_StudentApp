using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ChangeTimeScreen : Screen
    {
        private readonly BookingKind _kind;
        private readonly string _id;
        private int _day;
        private DateTime _start;
        private int _duration;

        public ChangeTimeScreen(DemoApp app, BookingKind kind, string id) : base(app)
        {
            _kind = kind;
            _id = id;
            _start = TimeOptions.Slots(App.Clock.Now, 0)[0];
            _duration = kind == BookingKind.Parking
                ? App.Store.ParkingReservations.First(p => p.Id == id).DurationMinutes
                : App.Store.ChargingSessions.First(s => s.Id == id).DurationMinutes;
        }

        public override string? Title => "Đổi giờ";

        private int[] Durations => _kind == BookingKind.Parking ? new[] { 60, 120, 180, 240 } : new[] { 60, 90, 120, 150, 180, 240 };

        public override UIElement BuildContent()
        {
            var item = App.Reservations.FindActive(_kind, _id);
            if (item == null) return EmptyState(Icons.Calendar, "Đặt chỗ không còn hoạt động", "Không thể đổi giờ.");
            var current = item.Rows.FirstOrDefault(r => r.Label == "Thời gian").Value ?? "-";
            return Stack(16,
                Card(Row("Đặt chỗ", item.Title), Row("Khung giờ hiện tại", current)),
                Labeled("Ngày", ChipGroup(Enumerable.Range(0, TimeOptions.Days.Length), _day, i => TimeOptions.Days[i], i =>
                {
                    _day = i;
                    _start = TimeOptions.Slots(App.Clock.Now, i)[0];
                    Shell.Refresh();
                })),
                Labeled("Giờ bắt đầu mới", ChipGroup(TimeOptions.Slots(App.Clock.Now, _day), _start, t => DemoConfig.Time(t), t =>
                {
                    _start = t;
                    Shell.Refresh();
                })),
                Labeled("Thời lượng", ChipGroup(Durations, _duration, d => ScreenHelpers.Duration(d), d =>
                {
                    _duration = d;
                    Shell.Refresh();
                })),
                Banner(Tone.Info, $"Khung giờ mới: {_start:dd/MM} · {DemoConfig.Time(_start)}–{DemoConfig.Time(_start.AddMinutes(_duration))}. Hệ thống sẽ kiểm tra tài nguyên còn trống trước khi cập nhật."));
        }

        public override UIElement? BuildActions() => Button("Kiểm tra và cập nhật", ButtonVariant.Primary, Submit);

        private void Submit()
        {
            var result = App.Reservations.ChangeTime(_kind, _id, _start, _duration);
            if (result.Ok)
            {
                Shell.Back();
                Shell.ShowDialog(Tone.Success, "Đã đổi giờ", $"Khung giờ mới: {DemoConfig.Time(_start)}–{DemoConfig.Time(_start.AddMinutes(_duration))} ngày {_start:dd/MM}. Thông báo xác nhận đã được gửi.");
                return;
            }
            Shell.ShowDialog(Tone.Warning, "Không thể đổi giờ", result.Message,
                new DialogAction("Chọn giờ khác", ButtonVariant.Primary),
                new DialogAction("Giữ lịch cũ", ButtonVariant.Outline, Shell.Back));
        }
    }
}
