using SmartEMobilityHub.Core.Adapters;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core;

/// <summary>
/// Một phiên mạng lưới dùng chung (DC-54): một <see cref="NetworkStore"/>, một đồng hồ mô phỏng và đủ service Core.
/// App Sinh viên và Trung tâm điều hành cùng đọc/ghi phiên này, nên không còn hai bộ dữ liệu phải đồng bộ.
/// Không phụ thuộc WPF: test tạo phiên với <see cref="FakeClock"/> làm "giờ thật".
/// Chu kỳ quét 6 bước do người gọi kích hoạt (<see cref="Scan"/>), không có luồng nền (DC-47).
/// </summary>
public sealed class NetworkSession
{
    /// <summary>Mốc giờ của Trung tâm điều hành khi chạy độc lập (kịch bản demo và ảnh chụp cố định).</summary>
    public static DateTime StandaloneStart => new(2026, 11, 21, 9, 41, 0);

    /// <summary>Hub có 3 yêu cầu sạc mẫu (HUB-08 là hero entity của màn hình Incident).</summary>
    public const string ChargingDemoHubId = "HUB-08";

    public NetworkSession(SimulationClock clock)
    {
        Clock = clock;
        Store = new NetworkStore(Clock);
        NetworkSeed.Apply(Store, Clock.Now);
        CampusFleetSeed.Apply(Store, Clock.Now);

        Log = new SystemLog(Clock);
        Gateway = new MockIoTSensorGateway(Store, Clock) { SimulateBatteryDrift = false };
        Notification = new InAppNotificationService();
        RetryQueue = new AlertRetryQueue(Notification, Clock, Log);
        Sync = new HubStatusSyncService(Store, Gateway, Clock, Log, RetryQueue);
        Threshold = new ThresholdMonitor(Store, Clock, Log);
        Reservations = new InMemoryReservationAdapter(Store, Clock);
        AutoCancel = new AutoCancelService(Reservations, Notification, Clock, Log);
        Charging = new SmartChargingScheduler(Store, Gateway, Clock, RetryQueue, Log);
        Incidents = new IncidentManager(Store, Notification, Reservations, Clock, Log);
        Timer = new StatusUpdaterTimer(
            Store, Clock, Sync, Threshold, RetryQueue, AutoCancel, Charging, Log, new ManualScheduler());
        Redistributor = new VehicleRedistributor(Store, Clock);
        WhatIf = new WhatIfController(new TempStateRepository(Store, Clock), new WhatIfEngineService(Clock), Store.Snapshot);
        StudentGateway = new StudentAppGateway(Store, Incidents, Charging, Reservations, Clock, AutoCancel);
    }

    /// <summary>Phiên của Trung tâm điều hành khi mở riêng: mốc giờ cố định, thời gian vẫn trôi theo giờ thật.</summary>
    public static NetworkSession CreateStandalone() => new(new SimulationClock(start: StandaloneStart));

    public SimulationClock Clock { get; }
    public NetworkStore Store { get; }
    public SystemLog Log { get; }
    public MockIoTSensorGateway Gateway { get; }
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

    /// <summary>Thao tác của app Sinh viên trên mạng lưới (đặt / nhận / trả xe, báo lỗi, sạc, chỗ đỗ).</summary>
    public StudentAppGateway StudentGateway { get; }

    public void Scan() => Timer.TickNow();

    public void Advance(int minutes) => Clock.AdvanceMinutes(minutes);

    // ---------- Sự kiện demo của Trung tâm điều hành ----------

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
        if (!Store.ChargingRequests.Any(r => r.RequestId.StartsWith("CR-DEMO-", StringComparison.Ordinal)))
        {
            AddRequest("CR-DEMO-1", "XE-129", 12, 20);
            AddRequest("CR-DEMO-2", "XE-139", 35, 60);
            AddRequest("CR-DEMO-3", "XE-128", 55, 90);
        }

        var plan = Charging.RunSchedule(Store.GetHub(ChargingDemoHubId));
        return plan.OrderedRequests.Count(r => r.AssignedPortId is not null);
    }

    private void AddRequest(string id, string vehicle, int battery, int neededInMinutes)
    {
        Store.ChargingRequests.Add(new ChargingRequest
        {
            RequestId = id,
            VehicleId = vehicle,
            HubId = ChargingDemoHubId,
            BatteryLevel = battery,
            NeededAt = Clock.Now.AddMinutes(neededInMinutes),
            PeakDemand = ServiceConstants.DefaultPeakDemand,
            QueuePosition = Store.ChargingRequests.Count
        });
    }
}
