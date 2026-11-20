using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ParkingFullScreen : Screen
    {
        private readonly ReserveParkingScreen _form;
        private readonly string _hubId;
        private readonly string _message;

        public ParkingFullScreen(ReserveParkingScreen form, string hubId, string message)
        {
            _form = form;
            _hubId = hubId;
            _message = message;
        }

        public override string? Title => "Đặt chỗ đỗ xe";

        public override UIElement BuildContent()
        {
            var hub = DemoApp.Store.GetHub(_hubId);
            var root = Stack(16, Banner(Tone.Warning, _message), Section($"Hub trong bán kính {DemoConfig.AlternativeHubRadiusKm:0} km còn chỗ"));
            var hubs = DemoApp.Parking.FindAvailableHubsWithin1Km(_hubId);
            if (hubs.Count == 0) Add(root, Text("Không có Hub nào trong bán kính 1 km còn chỗ trống.", TextKind.Body, Theme.Text2), 12);
            foreach (var h in hubs)
            {
                var id = h.Id;
                Add(root, HubCard(h.Name, $"{h.FreeSlots} chỗ trống trong khung giờ", DemoConfig.Km(h.DistanceKmTo(hub)), () => _form.Submit(id, true)), 12);
            }
            return root;
        }

        public override UIElement? BuildActions() => Columns(12,
            Button("Đổi thời gian", ButtonVariant.Outline, Shell.Back),
            Button("Dừng", ButtonVariant.DangerOutline, ScreenHelpers.GoHome));
    }
}
