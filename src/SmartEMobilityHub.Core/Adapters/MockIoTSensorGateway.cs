using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Adapters;

/// <summary>Loại lỗi có thể bơm vào gateway (dùng cho test và DemoControlPanel).</summary>
public enum FaultKind
{
    Timeout,
    Fault,
    Unreachable
}

/// <summary>
/// Adapter giả lập của IIoTSensorGateway (PLAN §4.3). Không gọi dịch vụ ngoài:
/// - Fault injection: <see cref="FailNext"/>, <see cref="SetStale"/>, <see cref="SetNetworkDown"/>.
/// - Mỗi lần lấy telemetry, pin xe CHARGING tăng và xe IN_USE giảm (mô phỏng cảm biến).
/// Lỗi ở một cổng chỉ làm cổng đó OFFLINE/FAULTY; lỗi ở Hub mới ném exception.
/// </summary>
public sealed class MockIoTSensorGateway : IIoTSensorGateway
{
    private readonly NetworkStore _store;
    private readonly IClock _clock;
    private readonly Dictionary<string, FaultKind> _nextFailures = new();
    private bool _networkDown;

    public MockIoTSensorGateway(NetworkStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>Tắt drift pin cho test cần dữ liệu cố định.</summary>
    public bool SimulateBatteryDrift { get; set; } = true;

    /// <summary>Lần gọi kế tiếp tới thiết bị này sẽ lỗi (một lần).</summary>
    public void FailNext(string deviceId, FaultKind kind) => _nextFailures[deviceId] = kind;

    /// <summary>Đánh dấu dữ liệu của thiết bị đã cũ N phút (NFR 2.2.3).</summary>
    public void SetStale(string deviceId, int minutes)
    {
        var staleAt = _clock.Now.AddMinutes(-minutes);
        var point = _store.FindChargingPoint(deviceId);
        if (point is not null)
        {
            point.LastUpdatedAt = staleAt;
            return;
        }

        var hub = _store.FindHub(deviceId);
        if (hub is not null)
        {
            hub.LastUpdatedAt = staleAt;
            return;
        }

        var vehicle = _store.FindVehicle(deviceId);
        if (vehicle is not null) vehicle.LastUpdatedAt = staleAt;
    }

    /// <summary>Ngắt toàn bộ mạng cảm biến (dùng cho demo hàng đợi retry).</summary>
    public void SetNetworkDown(bool down) => _networkDown = down;

    public HubTelemetry FetchSensorTelemetry(string hubId)
    {
        if (_networkDown) throw new SensorUnreachableException($"Mất kết nối tới gateway của {hubId}.");

        var hub = _store.GetHub(hubId);

        if (_nextFailures.Remove(hubId, out var hubFault))
            throw Map(hubFault, hubId);

        var capturedAt = _clock.Now;
        var vehicles = new List<VehicleTelemetry>();
        foreach (var vehicle in _store.Vehicles.Where(v => v.HubId == hubId).ToList())
        {
            var battery = vehicle.BatteryLevel;
            if (SimulateBatteryDrift)
            {
                if (vehicle.Status == DeviceStatus.Charging) battery = Math.Min(100, battery + 3);
                else if (vehicle.Status == DeviceStatus.InUse) battery = Math.Max(0, battery - 2);
            }

            vehicles.Add(new VehicleTelemetry
            {
                VehicleCode = vehicle.Code,
                BatteryLevel = battery,
                Status = vehicle.Status,
                Slot = vehicle.Slot
            });
        }

        var ports = new List<PortTelemetry>();
        foreach (var point in hub.ChargingPoints)
        {
            var status = point.Status;
            var temperature = point.TemperatureC;
            var lastUpdatedAt = capturedAt;

            if (_nextFailures.Remove(point.Id, out var pointFault))
            {
                if (pointFault == FaultKind.Fault)
                {
                    status = DeviceStatus.Faulty;
                    temperature = Math.Max(temperature, 70);
                }
                else
                {
                    status = DeviceStatus.Offline;
                    lastUpdatedAt = point.LastUpdatedAt; // không làm mới dữ liệu cũ
                }
            }
            else if (status == DeviceStatus.Offline)
            {
                lastUpdatedAt = point.LastUpdatedAt;
            }

            ports.Add(new PortTelemetry
            {
                PortId = point.Id,
                Status = status,
                TemperatureC = temperature,
                PowerKw = point.PowerKw,
                LastUpdatedAt = lastUpdatedAt
            });
        }

        return new HubTelemetry
        {
            HubId = hubId,
            CapturedAt = capturedAt,
            Vehicles = vehicles,
            Ports = ports
        };
    }

    public PowerCommandResult SendPowerCommand(string portId, double powerKw)
    {
        if (_networkDown) return PowerCommandResult.Fault($"Không gửi được lệnh tới {portId} (mất mạng).");

        if (_nextFailures.Remove(portId, out var fault))
        {
            return fault == FaultKind.Timeout
                ? PowerCommandResult.Timeout($"Cổng {portId} không phản hồi sau 30 giây.")
                : PowerCommandResult.Fault($"Cổng {portId} báo lỗi khi cấp {powerKw:0.#} kW.");
        }

        return PowerCommandResult.Ack($"Cổng {portId} nhận {powerKw:0.#} kW.");
    }

    private static Exception Map(FaultKind kind, string deviceId) => kind switch
    {
        FaultKind.Timeout => new GatewayTimeoutException($"Gateway {deviceId} quá thời gian chờ."),
        _ => new SensorUnreachableException($"Không kết nối được tới {deviceId}.")
    };
}
