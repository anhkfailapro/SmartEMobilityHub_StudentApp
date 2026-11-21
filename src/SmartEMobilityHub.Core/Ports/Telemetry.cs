using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Ports;

/// <summary>Dữ liệu cảm biến một chiếc xe tại Hub (UC-SYS-02).</summary>
public sealed class VehicleTelemetry
{
    public string VehicleCode { get; init; } = "";
    public int BatteryLevel { get; init; }
    public DeviceStatus Status { get; init; } = DeviceStatus.Available;
    public string? Slot { get; init; }
}

/// <summary>Dữ liệu cảm biến một cổng sạc.</summary>
public sealed class PortTelemetry
{
    public string PortId { get; init; } = "";
    public DeviceStatus Status { get; init; } = DeviceStatus.Available;
    public double TemperatureC { get; init; }
    public double PowerKw { get; init; }

    /// <summary>Mốc dữ liệu của cổng — giữ nguyên khi cổng mất kết nối để UI báo "dữ liệu cũ".</summary>
    public DateTime LastUpdatedAt { get; init; }
}

/// <summary>Gói telemetry của một Hub trong một chu kỳ quét.</summary>
public sealed class HubTelemetry
{
    public string HubId { get; init; } = "";
    public DateTime CapturedAt { get; init; }
    public IReadOnlyList<VehicleTelemetry> Vehicles { get; init; } = Array.Empty<VehicleTelemetry>();
    public IReadOnlyList<PortTelemetry> Ports { get; init; } = Array.Empty<PortTelemetry>();
}

/// <summary>Kết quả ACK/Fault/Timeout của lệnh cấp nguồn (UC-SYS-01 Exception 5a).</summary>
public sealed class PowerCommandResult
{
    public PowerCommandOutcome Outcome { get; init; }
    public string Message { get; init; } = "";

    public static PowerCommandResult Ack(string message = "ACK") =>
        new() { Outcome = PowerCommandOutcome.Ack, Message = message };

    public static PowerCommandResult Fault(string message) =>
        new() { Outcome = PowerCommandOutcome.Fault, Message = message };

    public static PowerCommandResult Timeout(string message) =>
        new() { Outcome = PowerCommandOutcome.Timeout, Message = message };
}
