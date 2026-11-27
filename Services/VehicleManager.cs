using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class VehicleManager
    {
        private readonly StudentContext Ctx;

        public VehicleManager(StudentContext ctx) => Ctx = ctx;

        public List<Vehicle> ListVehicles(string hubId) => Ctx.Store.Vehicles.Where(v => v.HubId == hubId).ToList();

        public int AvailableCount(Hub hub) => ListVehicles(hub.Id).Count(v => v.IsAvailable);

        public List<Hub> NearestHubsWithAvailable(Hub from, int count) =>
            Ctx.Store.Hubs.Where(h => h.Id != from.Id && !h.IsSuspended && AvailableCount(h) > 0).OrderBy(h => h.DistanceKmTo(from)).Take(count).ToList();

        public Vehicle? GetDetails(string code) => Ctx.Store.GetVehicle(code);

        public bool IsReadOnly(Vehicle vehicle) => !vehicle.IsAvailable;

        public void MakeStale(string code)
        {
            if (GetDetails(code) != null) Ctx.Gateway.MarkVehicleStale(code, 14);
        }
    }
}
