using SmartEMobilityHub.Core.Adapters;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Core.Store;
using SmartEMobilityHub.Operator.Infrastructure;

namespace SmartEMobilityHub.Operator;

/// <summary>
/// Một phiên demo độc lập: seed mới, FakeClock, đủ 6 service của GĐ2–4.
/// UI điều khiển chu kỳ quét bằng <see cref="Scan"/> (không dùng thread nền).
/// </summary>
public sealed class OperatorDemo
{
    public static DateTime Start => new(2026, 11, 21, 9, 41, 0);

    public FakeClock Clock { get; }
    public NetworkStore Store { get; }
    public MockIoTSensorGateway Gateway { get; }
    public UiLogger Logger { get; }
    public InAppNotificationService Notification { get; }
    public AlertRetryQueue RetryQueue { get; }
    public HubStatusSyncService Sync { get; }
    public ThresholdMonitor Threshold { get; }
    public InMemoryReservationAdapter Reservations { get; }
    public AutoCancelService AutoCancel { get; }
    public SmartChargingScheduler Charging { get; }
    public IncidentManager Incidents { get; }
    public StatusUpdaterTimer Timer { get; }
    public VehicleRedistributor Redistributor { get; }
    public WhatIfController WhatIf { get; }

    public OperatorDemo()
    {
        Clock = new FakeClock(Start);
        Store = new NetworkStore(Clock);
        NetworkSeed.Apply(Store, Clock.Now);

        Gateway = new MockIoTSensorGateway(Store, Clock) { SimulateBatteryDrift = false };
        Logger = new UiLogger();
        Notification = new InAppNotificationService();
        RetryQueue = new AlertRetryQueue(Notification, Clock, Logger);
        Sync = new HubStatusSyncService(Store, Gateway, Clock, Logger, RetryQueue);
        Threshold = new ThresholdMonitor(Store, Clock, Logger);
        Reservations = new InMemoryReservationAdapter(Store, Clock);
        AutoCancel = new AutoCancelService(Reservations, Notification, Clock, Logger);
        Charging = new SmartChargingScheduler(Store, Gateway, Clock, RetryQueue, Logger);
        Incidents = new IncidentManager(Store, Notification, Reservations, Clock, Logger);

        Timer = new StatusUpdaterTimer(
            Store, Clock, Sync, Threshold, RetryQueue, AutoCancel, Charging, Logger, new NoopScheduler());

        Redistributor = new VehicleRedistributor(Store, Clock);
        WhatIf = new WhatIfController(new TempStateRepository(Store), new WhatIfEngineService(), Store.Snapshot);
    }

    public void Scan() => Timer.TickNow();

    public void Advance(int minutes) => Clock.AdvanceMinutes(minutes);

    public void BreakChargingPoint(string pointId, string hubId)
    {
        Gateway.FailNext(pointId, FaultKind.Fault);
        Timer.TickNow();
        Incidents.ReceiveSensorAlert(hubId, pointId, AlertType.ChargerFault);
    }

    public void DisconnectHub(string hubId)
    {
        Gateway.FailNext(hubId, FaultKind.Unreachable);
        Timer.TickNow();
        Incidents.ReceiveSensorAlert(hubId, hubId, AlertType.DeviceOffline);
    }

    public void MakeStale(string hubId)
    {
        Gateway.SetStale(hubId, 14);
        Timer.CheckStaleData(Store.GetHub(hubId));
    }

    /// <summary>Giả lập mất mạng cảnh báo: alert HUB-08 vào hàng đợi, trả về số alert đang chờ.</summary>
    public int BreakAlertChannel()
    {
        Notification.NetworkDown = true;
        var alert = Threshold.EvaluateHub(Store.GetHub("HUB-08"));
        if (alert is null) return RetryQueue.Size();

        RetryQueue.DispatchAlert(alert);
        return RetryQueue.Size();
    }

    /// <summary>Khôi phục mạng, tua 31 giây và gửi lại — trả về số alert gửi thành công.</summary>
    public int RestoreAlertChannel()
    {
        Notification.NetworkDown = false;
        Clock.AdvanceSeconds(AlertRetryQueue.RetryDelayMs / 1000 + 1);
        return RetryQueue.ProcessQueue(Clock.Now);
    }

    public IncidentRecord RaiseUnlockTimeout() => Incidents.ReceiveUnlockTimeout("XE-121", "R-103");

    public int AddChargingRequests()
    {
        if (Store.ChargingRequests.Count == 0)
        {
            AddRequest("CR-DEMO-1", "XE-129", 12, 20);
            AddRequest("CR-DEMO-2", "XE-139", 35, 60);
            AddRequest("CR-DEMO-3", "XE-128", 55, 90);
        }

        var plan = Charging.RunSchedule(Store.GetHub("HUB-08"));
        return plan.OrderedRequests.Count(r => r.AssignedPortId is not null);
    }

    private void AddRequest(string id, string vehicle, int battery, int neededInMinutes)
    {
        Store.ChargingRequests.Add(new ChargingRequest
        {
            RequestId = id,
            VehicleId = vehicle,
            HubId = "HUB-08",
            BatteryLevel = battery,
            NeededAt = Clock.Now.AddMinutes(neededInMinutes),
            PeakDemand = ServiceConstants.DefaultPeakDemand,
            QueuePosition = Store.ChargingRequests.Count
        });
    }
}
