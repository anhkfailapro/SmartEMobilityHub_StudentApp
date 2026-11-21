using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// UC-SYS-02 — cập nhật trạng thái Hub/phương tiện (Sub3 Bảng 29).
/// Mỗi Hub được xử lý độc lập: timeout ở HUB-08 không làm dừng các Hub còn lại (NFR 2.2.2).
/// </summary>
public sealed class HubStatusSyncService
{
    private readonly NetworkStore _store;
    private readonly IIoTSensorGateway _gateway;
    private readonly IClock _clock;
    private readonly ISystemLogger _logger;
    private readonly IAlertSink _alertSink;

    public HubStatusSyncService(
        NetworkStore store,
        IIoTSensorGateway gateway,
        IClock clock,
        ISystemLogger logger,
        IAlertSink alertSink)
    {
        _store = store;
        _gateway = gateway;
        _clock = clock;
        _logger = logger;
        _alertSink = alertSink;
    }

    /// <summary>Trả về số Hub đồng bộ thành công trong chu kỳ.</summary>
    public int SyncHubStatus()
    {
        var synced = 0;

        foreach (var hub in _store.Hubs.ToList())
        {
            try
            {
                var telemetry = _gateway.FetchSensorTelemetry(hub.Id);
                Apply(hub, telemetry);
                synced++;
            }
            catch (Exception ex) when (ex is SensorUnreachableException or GatewayTimeoutException)
            {
                MarkOffline(hub, ex);
            }
            catch (Exception ex)
            {
                _logger.Error($"Lỗi không xác định khi đồng bộ {hub.Id}: {ex.Message}", "UC-SYS-02");
            }
        }

        return synced;
    }

    private void Apply(MobilityHub hub, HubTelemetry telemetry)
    {
        foreach (var vehicleTelemetry in telemetry.Vehicles)
        {
            var vehicle = _store.FindVehicle(vehicleTelemetry.VehicleCode);
            if (vehicle is null) continue;

            vehicle.BatteryLevel = vehicleTelemetry.BatteryLevel;
            vehicle.Status = vehicleTelemetry.Status;
            if (!string.IsNullOrWhiteSpace(vehicleTelemetry.Slot)) vehicle.Slot = vehicleTelemetry.Slot!;
            vehicle.LastUpdatedAt = telemetry.CapturedAt;
        }

        foreach (var portTelemetry in telemetry.Ports)
        {
            var point = hub.ChargingPoints.FirstOrDefault(p => p.Id == portTelemetry.PortId);
            if (point is null) continue;

            if (portTelemetry.Status == DeviceStatus.Offline && point.Status != DeviceStatus.Offline)
                _logger.Warn($"Cổng {point.Id} mất kết nối cảm biến.", "UC-SYS-02");

            point.Status = portTelemetry.Status;
            point.TemperatureC = portTelemetry.TemperatureC;
            point.LastUpdatedAt = portTelemetry.LastUpdatedAt;
        }

        hub.Connection = ConnectionStatus.Online;
        if (hub.Status == HubStatus.Offline) hub.Status = HubStatus.Online;
        hub.LastUpdatedAt = telemetry.CapturedAt;
    }

    private void MarkOffline(MobilityHub hub, Exception error)
    {
        hub.Connection = ConnectionStatus.Offline;
        if (hub.Status == HubStatus.Online) hub.Status = HubStatus.Offline;

        foreach (var point in hub.ChargingPoints) point.Status = DeviceStatus.Offline;

        // Không cập nhật LastUpdatedAt: giữ mốc cũ để UI hiển thị "Dữ liệu cũ · N phút".
        _logger.Error($"Mất kết nối cảm biến tại {hub.ShortName} ({hub.Id}): {error.Message}", "UC-SYS-02");

        _alertSink.DispatchAlert(new Alert
        {
            AlertId = _store.NextAlertId(),
            Type = AlertType.DeviceOffline,
            HubId = hub.Id,
            Message = $"Mất kết nối cảm biến tại {hub.ShortName} ({hub.Id}) — dữ liệu giữ nguyên mốc gần nhất.",
            CreatedAt = _clock.Now
        });
    }
}
