using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ReturnVehicleScreen : Screen
    {
        private readonly Action<Trip>? _afterReturn;
        private string _hubId;

        public ReturnVehicleScreen(DemoApp app, Action<Trip>? afterReturn = null, string? hubId = null) : base(app)
        {
            _afterReturn = afterReturn;
            _hubId = hubId ?? App.Billing.ActiveTrip?.StartHubId ?? CampusMap.DefaultHubId;
        }

        public override string? Title => "Trả xe";

        public override UIElement BuildContent()
        {
            var trip = App.Billing.ActiveTrip;
            if (trip == null) return EmptyState(Icons.Bike, "Không có chuyến đi", "Chuyến đi đã kết thúc.");
            var hub = App.Store.GetHub(_hubId);
            var billing = App.Billing;
            return Stack(16,
                Text("Đỗ xe vào một chỗ trống tại Hub, sau đó chọn Hub bạn đang trả xe và xác nhận.", TextKind.Body, Theme.Text2),
                Labeled("Hub trả xe", ChipGroup(App.Store.Hubs, hub, h => h.ShortName, h =>
                {
                    _hubId = h.Id;
                    Shell.Refresh();
                })),
                Card(
                    Row("Chỗ trống", hub.FreeSlots > 0 ? $"{hub.FreeSlots} chỗ" : "Hết chỗ", hub.FreeSlots > 0 ? null : Theme.Danger),
                    Row("Thời gian đã đi", ScreenHelpers.Elapsed(App.Clock.Now - trip.StartTime)),
                    Row("Phí tạm tính", DemoConfig.Money(billing.CurrentFee(trip)))),
                Banner(Tone.Info, $"Phí tính theo thời gian ({DemoConfig.Money(DemoConfig.RentalPricePerMinute)}/phút). Xe pin dưới {DemoConfig.NeedsChargeBelow}% sẽ chuyển sang “Cần sạc”."));
        }

        public override UIElement? BuildActions() =>
            App.Billing.ActiveTrip == null ? null : Button("Xác nhận trả xe", ButtonVariant.Primary, () => DoReturn(_hubId, false));

        public void DoReturn(string hubId, bool redirected)
        {
            var check = App.Billing.CheckReturn(hubId, redirected);
            if (!check.Ok)
            {
                Shell.Navigate(new ReturnRefusedScreen(App, check.Error, check.Message, check.Value, _afterReturn));
                return;
            }
            var trip = App.Billing.Complete(hubId);
            if (_afterReturn != null)
                _afterReturn(trip);
            else
                Shell.ResetTo(new TripSummaryScreen(App, trip));
        }
    }
}
