using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Services;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class JourneyResultScreen : Screen
    {
        private readonly JourneyResult _result;

        public JourneyResultScreen(string destination)
        {
            _result = DemoApp.Journey.PlanJourney(destination);
        }

        public override string? Title => "Kết quả hành trình";

        public override UIElement BuildContent()
        {
            var root = Stack(16, Labeled("Điểm đến", Panel(Text(_result.Destination, TextKind.Subtitle))));
            if (!_result.RouteAvailable)
            {
                Add(root, Banner(Tone.Warning, "Không có dữ liệu lộ trình (dịch vụ bản đồ không phản hồi sau 5 giây). Hub được sắp xếp theo khoảng cách đường thẳng."), 16);
                foreach (var h in _result.HubsByStraightLine)
                {
                    var hub = h;
                    Add(root, HubCard(hub.Name, ScreenHelpers.HubMeta(hub), ScreenHelpers.DistanceFromMe(hub), () => Shell.Navigate(new VehicleListScreen(hub.Id))), 12);
                }
                return root;
            }

            var start = _result.StartHub!;
            var end = _result.EndHub!;
            Add(root, Card(
                Text("Hub xuất phát", TextKind.Caption, Theme.Text2),
                Text(start.Name, TextKind.Subtitle),
                Row("Đi bộ", $"{DemoConfig.Km(_result.WalkToStartKm)} · ~{JourneyPlanner.WalkMinutes(_result.WalkToStartKm)} phút"),
                Row("Xe sẵn sàng", $"{DemoApp.Vehicles.AvailableCount(start)} xe")), 16);
            Add(root, Card(
                Text("Hub đến", TextKind.Caption, Theme.Text2),
                Text(end.Name, TextKind.Subtitle),
                Row("Cách điểm đến", $"{DemoConfig.Km(_result.EndToDestinationKm)} · ~{JourneyPlanner.WalkMinutes(_result.EndToDestinationKm)} phút đi bộ"),
                Row("Chỗ trống để trả xe", end.FreeSlots > 0 ? $"{end.FreeSlots} chỗ" : "Hết chỗ", end.FreeSlots > 0 ? null : Theme.Danger)), 16);
            Add(root, Card(
                Row("Quãng đường đi xe", DemoConfig.Km(_result.RideKm)),
                Row("Thời gian đi xe ước tính", $"~{Math.Max(1, (int)Math.Ceiling(_result.RideKm * 4))} phút")), 16);
            return root;
        }

        public override UIElement? BuildActions()
        {
            if (!_result.RouteAvailable || _result.StartHub == null) return null;
            var hubId = _result.StartHub.Id;
            return Button("Chọn Hub xuất phát", ButtonVariant.Primary, () => Shell.Navigate(new VehicleListScreen(hubId)));
        }
    }
}
