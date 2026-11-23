using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Store;

/// <summary>
/// Kho dữ liệu mạng lưới trong bộ nhớ (đề bài cho phép hard-code, không dùng DB).
/// Thay cho NetworkStore trong PLAN bản TypeScript — cùng vai trò: observable + snapshot sâu.
/// </summary>
public sealed class NetworkStore
{
    private readonly IClock _clock;
    private int _alertSeq;
    private int _taskSeq;
    private int _incidentSeq = 1000;

    public NetworkStore(IClock? clock = null) => _clock = clock ?? new SystemClock();

    public List<MobilityHub> Hubs { get; } = new();
    public List<Vehicle> Vehicles { get; } = new();
    public List<Reservation> Reservations { get; } = new();
    public List<IncidentRecord> Incidents { get; } = new();
    public List<Technician> Technicians { get; } = new();
    public List<MaintenanceTask> MaintenanceTasks { get; } = new();
    public List<Alert> Alerts { get; } = new();
    public List<ChargingRequest> ChargingRequests { get; } = new();
    public List<Operator> Operators { get; } = new();
    public List<DispatchOrder> DispatchOrders { get; } = new();

    /// <summary>Bản sao sâu, chỉ đọc — không thể ghi ngược vào store.</summary>
    public NetworkSnapshot Snapshot() => new(
        _clock.Now,
        Hubs,
        Vehicles,
        Reservations,
        Incidents,
        Technicians,
        MaintenanceTasks,
        Alerts,
        ChargingRequests,
        Operators,
        DispatchOrders);

    public void Clear()
    {
        Hubs.Clear();
        Vehicles.Clear();
        Reservations.Clear();
        Incidents.Clear();
        Technicians.Clear();
        MaintenanceTasks.Clear();
        Alerts.Clear();
        ChargingRequests.Clear();
        Operators.Clear();
        _alertSeq = 0;
        _taskSeq = 0;
        _incidentSeq = 1000;
        DispatchOrders.Clear();
    }

    public MobilityHub? FindHub(string hubId) => Hubs.FirstOrDefault(h => h.Id == hubId);

    public MobilityHub GetHub(string hubId) =>
        FindHub(hubId) ?? throw new HubNotFoundException($"Không tìm thấy Hub {hubId}.");

    public Vehicle? FindVehicle(string code) =>
        Vehicles.FirstOrDefault(v => string.Equals(v.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

    public ChargingPoint? FindChargingPoint(string pointId) =>
        Hubs.SelectMany(h => h.ChargingPoints).FirstOrDefault(p => p.Id == pointId);

    public IncidentRecord? FindIncident(string incidentId) =>
        Incidents.FirstOrDefault(i => i.IncidentId == incidentId);

    public IncidentRecord GetIncident(string incidentId) =>
        FindIncident(incidentId) ?? throw new IncidentNotFoundException($"Không tìm thấy sự cố {incidentId}.");

    public string NextAlertId() => $"AL-{++_alertSeq:0000}";

    public string NextTaskId() => $"MT-{++_taskSeq:0000}";

    /// <summary>
    /// ID kế tiếp đi sau mã lớn nhất đang có (seed dùng SC-1011…SC-1034),
    /// không phụ thuộc bộ đếm khởi tạo.
    /// </summary>
    public string NextIncidentId()
    {
        var max = _incidentSeq;

        foreach (var incident in Incidents)
        {
            if (!incident.IncidentId.StartsWith("SC-", StringComparison.Ordinal)) continue;
            if (int.TryParse(incident.IncidentId.AsSpan(3), out var number) && number > max) max = number;
        }

        string id;
        do
        {
            id = $"SC-{++max}";
        } while (Incidents.Any(i => i.IncidentId == id));

        _incidentSeq = max;
        return id;
    }
}
