using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class JourneyPlanner
    {
        public static readonly (string Name, double X, double Y)[] Destinations =
        {
            ("Thư viện Trung tâm", 0.05, 0.85),
            ("KTX Khu A", -1.25, 0.05),
            ("Ga Metro Suối Tiên", 0.4, 0.05),
            ("Nhà văn hóa Sinh viên", 0.35, 1.55),
            ("Trường ĐH Bách khoa", -0.6, 1.2)
        };

        public JourneyResult PlanJourney(string destination)
        {
            var store = DemoApp.Store;
            var byLine = store.Hubs.OrderBy(h => h.DistanceKmTo(0, 0)).ToList();
            var dest = ResolveDestination(destination);
            if (DemoApp.Toggles.MapServiceTimeout)
                return new JourneyResult { Destination = dest.Name, RouteAvailable = false, HubsByStraightLine = byLine };

            var start = byLine.FirstOrDefault(h => DemoApp.Vehicles.AvailableCount(h) > 0) ?? byLine[0];
            var end = store.Hubs.OrderBy(h => h.DistanceKmTo(dest.X, dest.Y)).First();
            return new JourneyResult
            {
                Destination = dest.Name,
                RouteAvailable = true,
                StartHub = start,
                EndHub = end,
                WalkToStartKm = start.DistanceKmTo(0, 0),
                EndToDestinationKm = end.DistanceKmTo(dest.X, dest.Y),
                RideKm = Math.Round(start.DistanceKmTo(end) * 1.4 + 0.3, 1),
                HubsByStraightLine = byLine
            };
        }

        public (string Name, double X, double Y) ResolveDestination(string text)
        {
            var t = (text ?? "").Trim();
            if (t.Length > 0)
            {
                foreach (var d in Destinations)
                {
                    if (d.Name.Contains(t, StringComparison.OrdinalIgnoreCase) || t.Contains(d.Name, StringComparison.OrdinalIgnoreCase))
                        return d;
                }
            }
            return Destinations[0];
        }

        public static int WalkMinutes(double km) => Math.Max(1, (int)Math.Ceiling(km * 12.5));
    }
}
