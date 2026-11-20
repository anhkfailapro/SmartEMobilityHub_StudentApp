using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class TripSummaryScreen : Screen
    {
        private readonly Trip _trip;
        private readonly string? _nextReserveHubId;
        private readonly string? _note;

        public TripSummaryScreen(Trip trip, string? nextReserveHubId = null, string? note = null)
        {
            _trip = trip;
            _nextReserveHubId = nextReserveHubId;
            _note = note;
        }

        public override string? Title => "Hoàn tất chuyến đi";
        public override bool ShowBack => false;

        public override UIElement BuildContent()
        {
            var hub = DemoApp.Store.GetHub(_trip.EndHubId ?? _trip.StartHubId);
            var v = DemoApp.Store.GetVehicle(_trip.VehicleCode)!;
            var check = new System.Windows.Controls.Border { Width = 72, Height = 72, CornerRadius = new CornerRadius(36), Background = Theme.SuccessSubtle, Child = Icon(Icons.Check, 32, Theme.Success), HorizontalAlignment = HorizontalAlignment.Center };
            var (label, tone) = DemoConfig.VehicleStatusLabel(v.Status);
            var root = Stack(16,
                Stack(8, check, Centered(Text("Trả xe thành công", TextKind.Title)), Centered(Text($"{_trip.VehicleCode} đã khóa tại Slot {_trip.EndSlot} · {hub.Name}", TextKind.Body, Theme.Text2))));
            if (_note != null) Add(root, Banner(Tone.Charging, _note), 16);
            Add(root, InvoiceView.Build(_trip), 16);
            Add(root, Card(Row("Trạng thái xe sau khi trả", label, Theme.ToneBrushes(tone).Foreground)), 16);
            Add(root, Banner(Tone.Info, $"Biên lai đã được gửi vào mục Thông báo. Xe pin dưới {DemoConfig.NeedsChargeBelow}% sẽ được chuyển sang trạng thái “Cần sạc”."), 16);
            return root;
        }

        public override UIElement? BuildActions()
        {
            if (_nextReserveHubId != null)
            {
                var hubId = _nextReserveHubId;
                return Columns(12,
                    Button("Về trang chủ", ButtonVariant.Outline, ScreenHelpers.GoHome),
                    Button("Đặt xe mới", ButtonVariant.Primary, () =>
                    {
                        DemoApp.GoTab(NavTab.Home);
                        Shell.Navigate(new VehicleListScreen(hubId));
                    }));
            }
            var code = _trip.VehicleCode;
            return Columns(12,
                Button("Báo lỗi xe", ButtonVariant.Outline, () => Shell.Navigate(new ReportProblemScreen(ReportObjectKind.Vehicle, code))),
                Button("Về trang chủ", ButtonVariant.Primary, ScreenHelpers.GoHome));
        }
    }
}
