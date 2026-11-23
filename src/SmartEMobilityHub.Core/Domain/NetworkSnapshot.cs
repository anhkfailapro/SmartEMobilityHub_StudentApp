namespace SmartEMobilityHub.Core.Domain;

/// <summary>
/// Bản sao độc lập của toàn bộ mạng lưới tại một thời điểm (PLAN §2 — Đức dùng cho What-if).
/// Mọi entity đều được clone sâu: sửa snapshot không ảnh hưởng NetworkStore.
/// </summary>
public sealed class NetworkSnapshot
{
    public NetworkSnapshot(
        DateTime takenAt,
        IEnumerable<MobilityHub> hubs,
        IEnumerable<Vehicle> vehicles,
        IEnumerable<Reservation> reservations,
        IEnumerable<IncidentRecord> incidents,
        IEnumerable<Technician> technicians,
        IEnumerable<MaintenanceTask> maintenanceTasks,
        IEnumerable<Alert> alerts,
        IEnumerable<ChargingRequest> chargingRequests,
        IEnumerable<Operator> operators)
    {
        TakenAt = takenAt;
        Hubs = hubs.Select(h => h.Clone()).ToList();
        Vehicles = vehicles.Select(v => v.Clone()).ToList();
        Reservations = reservations.Select(r => r.Clone()).ToList();
        Incidents = incidents.Select(i => i.Clone()).ToList();
        Technicians = technicians.Select(t => t.Clone()).ToList();
        MaintenanceTasks = maintenanceTasks.Select(t => t.Clone()).ToList();
        Alerts = alerts.Select(a => a.Clone()).ToList();
        ChargingRequests = chargingRequests.Select(r => r.Clone()).ToList();
        Operators = operators.Select(o => o.Clone()).ToList();
    }

    public DateTime TakenAt { get; }
    public IReadOnlyList<MobilityHub> Hubs { get; }
    public IReadOnlyList<Vehicle> Vehicles { get; }
    public IReadOnlyList<Reservation> Reservations { get; }
    public IReadOnlyList<IncidentRecord> Incidents { get; }
    public IReadOnlyList<Technician> Technicians { get; }
    public IReadOnlyList<MaintenanceTask> MaintenanceTasks { get; }
    public IReadOnlyList<Alert> Alerts { get; }
    public IReadOnlyList<ChargingRequest> ChargingRequests { get; }
    public IReadOnlyList<Operator> Operators { get; }

    public MobilityHub? FindHub(string id) => Hubs.FirstOrDefault(h => h.Id == id);

    public Vehicle? FindVehicle(string code) =>
        Vehicles.FirstOrDefault(v => string.Equals(v.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

    public IncidentRecord? FindIncident(string id) => Incidents.FirstOrDefault(i => i.IncidentId == id);
}
