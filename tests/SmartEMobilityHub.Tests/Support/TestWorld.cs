using SmartEMobilityHub.Core.Adapters;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Tests.Support;

/// <summary>Một "thế giới" đã nối dây đầy đủ, seed cố định, cho mỗi test độc lập.</summary>
public sealed class TestWorld
{
    public static DateTime Start => new(2026, 11, 21, 9, 41, 0);

    public FakeClock Clock { get; }
    public NetworkStore Store { get; }
    public MockIoTSensorGateway Gateway { get; }
    public InAppNotificationService Notification { get; }
    public CollectingLogger Logger { get; }
    public AlertRetryQueue Queue { get; }
    public HubStatusSyncService Sync { get; }
    public ThresholdMonitor Threshold { get; }
    public InMemoryReservationAdapter Reservations { get; }
    public AutoCancelService AutoCancel { get; }
    public SmartChargingScheduler Charging { get; }
    public IncidentManager Incidents { get; }
    public StatusUpdaterTimer Timer { get; }

    private TestWorld()
    {
        Clock = new FakeClock(Start);
        Store = new NetworkStore(Clock);
        NetworkSeed.Apply(Store, Clock.Now);

        Gateway = new MockIoTSensorGateway(Store, Clock) { SimulateBatteryDrift = false };
        Notification = new InAppNotificationService();
        Logger = new CollectingLogger();
        Queue = new AlertRetryQueue(Notification, Clock, Logger);
        Sync = new HubStatusSyncService(Store, Gateway, Clock, Logger, Queue);
        Threshold = new ThresholdMonitor(Store, Clock, Logger);
        Reservations = new InMemoryReservationAdapter(Store, Clock);
        AutoCancel = new AutoCancelService(Reservations, Notification, Clock, Logger);
        Charging = new SmartChargingScheduler(Store, Gateway, Clock, Queue, Logger);
        Incidents = new IncidentManager(Store, Notification, Reservations, Clock, Logger);

        // Không truyền scheduler: test chỉ gọi TickNow(), không chạy timer nền.
        Timer = new StatusUpdaterTimer(Store, Clock, Sync, Threshold, Queue, AutoCancel, Charging, Logger);
    }

    public static TestWorld Create() => new();

    /// <summary>World có đường thông báo đang lỗi (NetworkDown = true).</summary>
    public static TestWorld CreateOffline()
    {
        var world = new TestWorld();
        world.Notification.NetworkDown = true;
        return world;
    }

    public static ChargingRequest AddRequest(
        TestWorld world,
        string id,
        string vehicleCode,
        string hubId,
        int battery,
        int neededInMinutes,
        double peakDemand = ServiceConstants.DefaultPeakDemand)
    {
        var request = new ChargingRequest
        {
            RequestId = id,
            VehicleId = vehicleCode,
            HubId = hubId,
            BatteryLevel = battery,
            NeededAt = world.Clock.Now.AddMinutes(neededInMinutes),
            PeakDemand = peakDemand,
            QueuePosition = world.Store.ChargingRequests.Count
        };

        world.Store.ChargingRequests.Add(request);
        return request;
    }
}
