using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Seed;

/// <summary>
/// Điểm vào duy nhất để tạo dữ liệu mô phỏng. Toàn bộ dữ liệu là deterministic:
/// cùng một mốc thời gian cho ra cùng một mạng lưới (PLAN §4).
/// </summary>
public static class NetworkSeed
{
    public static void Apply(NetworkStore store, DateTime now)
    {
        store.Clear();

        foreach (var hub in HubSeed.Build(now)) store.Hubs.Add(hub);

        VehicleSeed.Apply(store, now);
        ReservationSeed.Apply(store, now);
        IncidentSeed.Apply(store, now);
        TechnicianSeed.Apply(store);

        store.NotifyChanged();
    }

    /// <summary>Tạo store mới đã seed — tiện cho demo và test.</summary>
    public static NetworkStore CreateStore(DateTime now, IClock? clock = null)
    {
        var store = new NetworkStore(clock ?? new FakeClock(now));
        Apply(store, now);
        return store;
    }
}
