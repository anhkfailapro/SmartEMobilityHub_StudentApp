namespace SmartEMobilityHub.Core.Ports;

/// <summary>
/// Cổng kết nối dữ liệu cảm biến (NFR 2.2.6: đổi IoT thật/giả lập không sửa nghiệp vụ).
/// Adapter mặc định: MockIoTSensorGateway (GĐ2).
/// </summary>
public interface IIoTSensorGateway
{
    /// <summary>
    /// Lấy telemetry của một Hub. Ném SensorUnreachableException/GatewayTimeoutException
    /// khi mất kết nối để HubStatusSyncService chuyển thiết bị sang OFFLINE (UC-SYS-02 Exception 2a).
    /// </summary>
    HubTelemetry FetchSensorTelemetry(string hubId);

    /// <summary>Gửi lệnh cấp nguồn tới cổng sạc; trả ACK/Fault/Timeout (Bảng 37 allocatePower).</summary>
    PowerCommandResult SendPowerCommand(string portId, double powerKw);
}
