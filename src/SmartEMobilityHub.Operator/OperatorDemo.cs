using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Adapters;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Core.Store;
using SmartEMobilityHub.Operator.Infrastructure;

namespace SmartEMobilityHub.Operator;

/// <summary>
/// Trung tâm điều hành gắn với một <see cref="NetworkSession"/> (DC-54).
/// Chạy độc lập: phiên riêng với mốc giờ cố định. Mở từ app Sinh viên: dùng CHÍNH phiên của app Sinh viên,
/// nên hai bên đọc/ghi cùng một mạng lưới, không có bước đồng bộ.
/// Lớp này chỉ thêm phần hiển thị (nhật ký cho WPF); nghiệp vụ nằm ở Core.
/// </summary>
public sealed class OperatorDemo
{
    public OperatorDemo() : this(NetworkSession.CreateStandalone())
    {
    }

    public OperatorDemo(NetworkSession session)
    {
        Session = session;
        Logger = new UiLogger(session.Log);
    }

    public static DateTime Start => NetworkSession.StandaloneStart;

    /// <summary>Hub có 3 yêu cầu sạc mẫu (HUB-08 là hero entity của màn hình Incident).</summary>
    public const string ChargingDemoHubId = NetworkSession.ChargingDemoHubId;

    public NetworkSession Session { get; }

    public SimulationClock Clock => Session.Clock;
    public NetworkStore Store => Session.Store;
    public MockIoTSensorGateway Gateway => Session.Gateway;
    public UiLogger Logger { get; }
    public InAppNotificationService Notification => Session.Notification;
    public AlertRetryQueue RetryQueue => Session.RetryQueue;
    public HubStatusSyncService Sync => Session.Sync;
    public ThresholdMonitor Threshold => Session.Threshold;
    public InMemoryReservationAdapter Reservations => Session.Reservations;
    public AutoCancelService AutoCancel => Session.AutoCancel;
    public SmartChargingScheduler Charging => Session.Charging;
    public IncidentManager Incidents => Session.Incidents;
    public StatusUpdaterTimer Timer => Session.Timer;
    public VehicleRedistributor Redistributor => Session.Redistributor;
    public WhatIfController WhatIf => Session.WhatIf;
    public StudentAppGateway StudentGateway => Session.StudentGateway;

    public void Scan() => Session.Scan();
    public void Advance(int minutes) => Session.Advance(minutes);
    public void BreakChargingPoint(string pointId, string hubId) => Session.BreakChargingPoint(pointId, hubId);
    public void DisconnectHub(string hubId) => Session.DisconnectHub(hubId);
    public void MakeStale(string hubId) => Session.MakeStale(hubId);
    public int BreakAlertChannel() => Session.BreakAlertChannel();
    public int RestoreAlertChannel() => Session.RestoreAlertChannel();
    public IncidentRecord RaiseUnlockTimeout() => Session.RaiseUnlockTimeout();
    public int AddChargingRequests() => Session.AddChargingRequests();
}
