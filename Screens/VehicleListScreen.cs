using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class VehicleListScreen : Screen
    {
        private static readonly string[] Filters = { "Tất cả", "Pin > 50%", "Xe đạp điện", "Xe máy điện" };
        private readonly string _hubId;
        private string _filter = Filters[0];

        public VehicleListScreen(string hubId)
        {
            _hubId = hubId;
        }

        public override string? Title => DemoApp.Store.GetHub(_hubId).Name;

        public override UIElement BuildContent()
        {
            var hub = DemoApp.Store.GetHub(_hubId);
            var all = DemoApp.Vehicles.ListVehicles(_hubId);
            var available = all.Count(v => v.IsAvailable);
            var root = Stack(16, HStack(8, Icon(Icons.Clock, 14, Theme.Text2),
                Text($"Cập nhật lúc {DemoConfig.Time(DemoApp.Clock.Now)} · {available} xe sẵn sàng · cách bạn {ScreenHelpers.DistanceFromMe(hub)}", TextKind.Caption, Theme.Text2)));
            Add(root, ChipGroup(Filters, _filter, f => f, f =>
            {
                _filter = f;
                Shell.Refresh();
            }), 12);

            if (available == 0)
            {
                Add(root, Banner(Tone.Warning, $"Hiện không có xe sẵn sàng tại {hub.Name}."), 8);
                Add(root, Section($"{DemoConfig.NearestHubCount} Hub gần nhất còn xe"), 16);
                foreach (var h in DemoApp.Vehicles.NearestHubsWithAvailable(hub, DemoConfig.NearestHubCount))
                {
                    var other = h;
                    Add(root, HubCard(other.Name, $"{DemoApp.Vehicles.AvailableCount(other)} xe sẵn sàng", DemoConfig.Km(other.DistanceKmTo(hub)), () => Shell.Navigate(new VehicleListScreen(other.Id))), 12);
                }
                if (all.Count > 0) Add(root, Section("Xe khác tại Hub này"), 16);
            }

            var list = all.Where(Matches).OrderBy(v => v.IsAvailable ? 0 : 1).ThenByDescending(v => v.Battery).ToList();
            if (list.Count == 0 && all.Count > 0) Add(root, Text("Không có xe phù hợp bộ lọc.", TextKind.Body, Theme.Text2), 12);
            foreach (var v in list)
            {
                var code = v.Code;
                Add(root, VehicleCard(v, () => Shell.Navigate(new VehicleDetailScreen(code))), 12);
            }
            return root;
        }

        private bool Matches(Vehicle v) => _filter switch
        {
            "Pin > 50%" => v.Battery > 50,
            "Xe đạp điện" => v.Type == VehicleType.EBike,
            "Xe máy điện" => v.Type == VehicleType.EMotorbike,
            _ => true
        };
    }
}
