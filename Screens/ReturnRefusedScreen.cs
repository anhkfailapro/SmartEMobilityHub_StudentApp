using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ReturnRefusedScreen : Screen
    {
        private readonly string _error;
        private readonly string _message;
        private readonly Hub? _nearest;
        private readonly Action<Trip>? _afterReturn;

        public ReturnRefusedScreen(string error, string message, Hub? nearest, Action<Trip>? afterReturn)
        {
            _error = error;
            _message = message;
            _nearest = nearest;
            _afterReturn = afterReturn;
        }

        private bool HubFull => _error == "HubFull";

        public override string? Title => HubFull ? "Hub đã hết chỗ" : "Không thể trả xe";

        public override UIElement BuildContent()
        {
            var root = Stack(16, Banner(HubFull ? Tone.Warning : Tone.Danger, _message));
            if (_nearest == null)
            {
                Add(root, Text("Hiện không có Hub nào còn chỗ trống.", TextKind.Body, Theme.Text2), 16);
                return root;
            }
            Add(root, Section("Hub gần nhất còn chỗ trống"), 16);
            Add(root, HubCard(_nearest.Name, $"{_nearest.FreeSlots} chỗ trống", DemoConfig.Km(_nearest.DistanceKmTo(0, 0)), null), 12);
            var trip = DemoApp.Billing.ActiveTrip;
            if (HubFull && trip != null && trip.ExtraFreeMinutes > 0)
                Add(root, Banner(Tone.Success, $"Bạn được cộng thêm {trip.ExtraFreeMinutes} phút miễn phí để đến {_nearest.ShortName}."), 16);
            return root;
        }

        public override UIElement? BuildActions()
        {
            if (_nearest == null) return Button("Quay lại", ButtonVariant.Outline, Shell.Back);
            var hubId = _nearest.Id;
            return Stack(12,
                Button($"Đã đến {_nearest.ShortName} – trả xe tại đây", ButtonVariant.Primary, () => new ReturnVehicleScreen(_afterReturn, hubId).DoReturn(hubId, true)),
                Button("Quay lại", ButtonVariant.Outline, Shell.Back));
        }
    }
}
