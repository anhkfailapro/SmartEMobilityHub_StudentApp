using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class JourneyPlanner
    {
        private readonly StudentContext Ctx;

        public JourneyPlanner(StudentContext ctx) => Ctx = ctx;

        /// <summary>Điểm đến gợi ý (toạ độ theo bản đồ khu ĐHQG của CampusMap).</summary>
        public static (string Name, double X, double Y)[] Destinations => CampusMap.Destinations;

        public JourneyResult PlanJourney(string destination)
        {
            var store = Ctx.Store;
            var byLine = store.Hubs.OrderBy(h => h.DistanceKmTo(0, 0)).ToList();
            var dest = ResolveDestination(destination);
            if (Ctx.Toggles.MapServiceTimeout)
                return new JourneyResult { Destination = dest.Name, RouteAvailable = false, HubsByStraightLine = byLine };

            // Hub đang tạm dừng (Operator) không được chọn làm điểm nhận / trả xe.
            var open = byLine.Where(h => !h.IsSuspended).ToList();
            if (open.Count == 0) open = byLine;
            var start = open.FirstOrDefault(h => Ctx.Vehicles.AvailableCount(h) > 0) ?? open[0];
            var end = open.OrderBy(h => h.DistanceKmTo(dest.X, dest.Y)).First();
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
