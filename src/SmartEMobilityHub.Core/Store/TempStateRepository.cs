using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Store;

/// <summary>In-memory isolated session repository (Sub3 TempStateRepository; Redis stand-in for MVP).</summary>
public sealed class TempStateRepository
{
    private readonly NetworkStore _network;
    private readonly Dictionary<string, SimulationSession> _sessions = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public TempStateRepository(NetworkStore network) => _network = network;

    public SimulationSession CreateSession(ScenarioPreset preset, SimulationParameters parameters)
    {
        var session = new SimulationSession
        {
            SessionId = $"SIM-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Preset = preset,
            Parameters = parameters,
            CreatedAt = DateTime.Now
        };
        lock (_gate) _sessions.Add(session.SessionId, session);
        return session;
    }

    public NetworkSnapshot CloneCurrentNetworkState(string sessionId)
    {
        lock (_gate)
        {
            var session = GetSessionCore(sessionId);
            session.OriginalSnapshot ??= _network.Snapshot();
            return Clone(session.OriginalSnapshot);
        }
    }

    public NetworkSnapshot GetSnapshot(string sessionId)
    {
        lock (_gate)
        {
            var snapshot = GetSessionCore(sessionId).OriginalSnapshot
                           ?? throw new InvalidOperationException("Phiên mô phỏng chưa có snapshot.");
            return Clone(snapshot);
        }
    }

    public void SaveTemporaryResult(string sessionId, SimulationResult result)
    {
        lock (_gate)
        {
            var session = GetSessionCore(sessionId);
            session.Runs.Add(result);
            session.State = SimulationState.DisplayingResults;
        }
    }

    public void DeleteSnapshot(string sessionId)
    {
        lock (_gate)
        {
            var session = GetSessionCore(sessionId);
            session.OriginalSnapshot = null;
            session.State = SimulationState.Cancelled;
        }
    }

    public void CompleteSession(string sessionId)
    {
        lock (_gate)
        {
            var session = GetSessionCore(sessionId);
            session.OriginalSnapshot = null;
            session.State = SimulationState.Completed;
        }
    }

    public SimulationSession GetSession(string sessionId)
    {
        lock (_gate) return GetSessionCore(sessionId);
    }

    public IReadOnlyList<SimulationSession> GetHistory()
    {
        lock (_gate)
            return _sessions.Values.Where(s => s.Runs.Count > 0)
                .OrderByDescending(s => s.LatestResult!.StartedAt).ToList();
    }

    public SimulationSession CreateRerunSession(string sourceSessionId)
    {
        lock (_gate)
        {
            var source = GetSessionCore(sourceSessionId);
            var snapshot = source.OriginalSnapshot ?? source.LatestResult?.BeforeState
                ?? throw new InvalidOperationException("Phiên gốc không có snapshot để chạy lại.");
            var rerun = new SimulationSession
            {
                SessionId = $"SIM-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
                Preset = source.Preset,
                Parameters = source.Parameters,
                CreatedAt = DateTime.Now,
                OriginalSnapshot = Clone(snapshot)
            };
            _sessions.Add(rerun.SessionId, rerun);
            return rerun;
        }
    }

    private SimulationSession GetSessionCore(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session)
            ? session
            : throw new KeyNotFoundException($"Không tìm thấy phiên mô phỏng {sessionId}.");

    private static NetworkSnapshot Clone(NetworkSnapshot snapshot) => new(
        snapshot.TakenAt, snapshot.Hubs, snapshot.Vehicles, snapshot.Reservations, snapshot.Incidents,
        snapshot.Technicians, snapshot.MaintenanceTasks, snapshot.Alerts, snapshot.ChargingRequests,
        snapshot.Operators);
}
