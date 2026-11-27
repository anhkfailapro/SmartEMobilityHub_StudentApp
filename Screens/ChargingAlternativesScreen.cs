using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ChargingAlternativesScreen : Screen
    {
        private readonly ScheduleChargingScreen _form;
        private readonly string _hubId;
        private readonly DateTime _start;
        private readonly string _message;

        public ChargingAlternativesScreen(DemoApp app, ScheduleChargingScreen form, string hubId, DateTime start, string message) : base(app)
        {
            _form = form;
            _hubId = hubId;
            _start = start;
            _message = message;
        }

        public override string? Title => "Lên lịch sạc";

        public override UIElement BuildContent()
        {
            var root = Stack(16, Banner(Tone.Warning, _message), Section("Khung giờ trống gần nhất"));
            foreach (var (hub, start) in App.Booking.FindAlternativeWindows(_hubId, _start))
            {
                var id = hub.Id;
                var st = start;
                var same = id == _hubId ? "Cùng Hub" : "Hub khác";
                Add(root, HubCard(hub.Name,
                    $"{same} · {st:dd/MM} {DemoConfig.Time(st)}–{DemoConfig.Time(st.AddMinutes(_form.Duration))} · tối đa {hub.MaxChargingMinutes} phút",
                    DemoConfig.Km(hub.DistanceKmTo(0, 0)),
                    () => _form.Submit(id, st, true)), 12);
            }
            return root;
        }

        public override UIElement? BuildActions() => Columns(12,
            Button("Chọn lại", ButtonVariant.Outline, Shell.Back),
            Button("Dừng", ButtonVariant.DangerOutline, GoHome));
    }
}
