using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class VehicleManager
    {
        public List<Vehicle> ListVehicles(string hubId) => DemoApp.Store.Vehicles.Where(v => v.HubId == hubId).ToList();

        public int AvailableCount(Hub hub) => ListVehicles(hub.Id).Count(v => v.IsAvailable);

        public List<Hub> NearestHubsWithAvailable(Hub from, int count) =>
            DemoApp.Store.Hubs.Where(h => h.Id != from.Id && !h.IsSuspended && AvailableCount(h) > 0).OrderBy(h => h.DistanceKmTo(from)).Take(count).ToList();

        public Vehicle? GetDetails(string code) => DemoApp.Store.GetVehicle(code);

        public bool IsReadOnly(Vehicle vehicle) => !vehicle.IsAvailable;

        public void MakeStale(string code)
        {
            var v = GetDetails(code);
            if (v != null) v.LastUpdate = DemoApp.Clock.Now.AddMinutes(-14);
        }
    }
}
