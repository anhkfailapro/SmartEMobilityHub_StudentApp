using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Infrastructure;

namespace SmartEMobilityHub.Operator;

/// <summary>
/// Kịch bản demo của Trung tâm điều hành trên một <see cref="NetworkSession"/>.
/// Lớp này chỉ giữ hai việc: các lệnh kịch bản (bơm sự kiện IoT, làm lỗi kênh cảnh báo, tua giờ…) với đối tượng mẫu cố định,
/// và nhật ký hiển thị cho WPF. Mọi service nghiệp vụ đọc thẳng từ <see cref="Session"/>.
/// Chạy độc lập: phiên riêng với mốc giờ cố định. Mở từ Launcher: dùng CHÍNH phiên của app Sinh viên.
/// </summary>
public sealed class OperatorDemo
{
    /// <summary>Hub có 3 yêu cầu sạc mẫu (HUB-08 là hero entity của màn hình Incident).</summary>
    public const string ChargingDemoHubId = NetworkSession.ChargingDemoHubId;

    public const string DemoBrokenPointId = "CS-08-08";
    public const string DemoBrokenPointHubId = "HUB-08";
    public const string DemoDisconnectedHubId = "HUB-03";
    public const string DemoStaleHubId = "HUB-08";

    public OperatorDemo() : this(NetworkSession.CreateStandalone())
    {
    }

    public OperatorDemo(NetworkSession session)
    {
        Session = session;
        Logger = new UiLogger(session.Log);
    }

    public NetworkSession Session { get; }

    /// <summary>Nhật ký của phiên, hiển thị ở trang "Kịch bản demo &amp; nhật ký".</summary>
    public UiLogger Logger { get; }

    public void Advance(int minutes) => Session.Advance(minutes);
    public void BreakDemoPort() => Session.BreakChargingPoint(DemoBrokenPointId, DemoBrokenPointHubId);
    public void DisconnectDemoHub() => Session.DisconnectHub(DemoDisconnectedHubId);
    public void MakeDemoHubStale() => Session.MakeStale(DemoStaleHubId);
    public int BreakAlertChannel() => Session.BreakAlertChannel();
    public int RestoreAlertChannel() => Session.RestoreAlertChannel();
    public IncidentRecord RaiseUnlockTimeout() => Session.RaiseUnlockTimeout();
    public int AddChargingRequests() => Session.AddChargingRequests();
}
