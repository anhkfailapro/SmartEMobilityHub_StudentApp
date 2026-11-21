using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Ports;

/// <summary>
/// Nơi nhận cảnh báo vận hành. AlertRetryQueue hiện thực interface này để mọi cảnh báo
/// đều đi qua cơ chế gửi lại sau 30 giây (UC-SYS-03 Exception 4a, NFR 2.2.2).
/// </summary>
public interface IAlertSink
{
    /// <summary>Trả true nếu gửi được ngay, false nếu đã đưa vào hàng đợi.</summary>
    bool DispatchAlert(Alert alert);
}
