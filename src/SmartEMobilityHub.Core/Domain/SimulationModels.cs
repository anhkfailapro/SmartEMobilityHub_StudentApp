namespace SmartEMobilityHub.Core.Domain;

/// <summary>UC-WI-01: five scenario families defined in Submission 1.</summary>
public enum ScenarioType
{
    MetroDemandSurge,
    ParkingFull,
    ChargingDemandSurge,
    ChargerOutage,
    VehicleConcentration
}

public enum SimulationState
{
    Configuring,
    Simulating,
    Evaluating,
    DisplayingResults,
    ReSimulating,
    Completed,
    Cancelled,
    Failed
}

public enum RecommendationAction
{
    RedirectDemand,
    RedistributeVehicles,
    PrioritizeLowBattery
}

public sealed record SimulationParameters
{
    public string TargetHubId { get; init; } = "";
    public double DemandSurgePercent { get; init; } = 50;
    public DateTime StartTime { get; init; } = new(2026, 11, 21, 14, 0, 0);
    public DateTime EndTime { get; init; } = new(2026, 11, 21, 19, 0, 0);
    public int TimeStepMinutes { get; init; } = 30;
    public int DisabledChargerCount { get; init; } = 1;
}

public sealed record ScenarioPreset(
    string Id,
    string Name,
    string Description,
    ScenarioType Type,
    double DefaultDemandSurgePercent = 50,
    int DefaultDisabledChargerCount = 1)
{
    public SimulationParameters GetDefaultConfig(string targetHubId, DateTime? date = null)
    {
        var day = (date ?? DateTime.Today).Date;
        return new SimulationParameters
        {
            TargetHubId = targetHubId,
            DemandSurgePercent = DefaultDemandSurgePercent,
            StartTime = day.AddHours(14),
            EndTime = day.AddHours(19),
            DisabledChargerCount = DefaultDisabledChargerCount
        };
    }

    public ValidationResult ValidateCustomValues(SimulationParameters parameters, NetworkSnapshot snapshot)
    {
        if (Type == ScenarioType.ChargerOutage)
        {
            var hub = snapshot.FindHub(parameters.TargetHubId);
            var total = hub?.ChargingPoints.Count(p =>
                p.Status is not DeviceStatus.Offline and not DeviceStatus.Faulty) ?? 0;
            if (parameters.DisabledChargerCount < 1 || parameters.DisabledChargerCount > total)
                return ValidationResult.Invalid($"Số cổng sạc hỏng phải từ 1 đến {total}.");
        }

        return ValidationResult.Valid();
    }
}

public sealed record ValidationResult(bool IsValid, string? Error)
{
    public static ValidationResult Valid() => new(true, null);
    public static ValidationResult Invalid(string error) => new(false, error);
}

public sealed record HubImpact(
    string HubId,
    string HubName,
    int OccupiedBefore,
    int OccupiedAfter,
    int Capacity,
    double ParkingUtilizationBefore,
    double ParkingUtilizationAfter,
    double ChargingUtilizationBefore,
    double ChargingUtilizationAfter,
    int AvailableVehiclesBefore,
    int AvailableVehiclesAfter,
    int ChargingPortsBusyBefore,
    int ChargingPortsBusyAfter,
    int EstimatedChargingWaitBeforeMinutes,
    int EstimatedChargingWaitAfterMinutes,
    bool IsOverloaded,
    bool IsCritical)
{
    public double OverallUtilizationBefore => Math.Max(ParkingUtilizationBefore, ChargingUtilizationBefore);
    public double OverallUtilizationAfter => Math.Max(ParkingUtilizationAfter, ChargingUtilizationAfter);
    public string ChargingPortsComparison => $"{ChargingPortsBusyBefore}/{ChargingPortsBusyAfter}";
    public string ChargingWaitComparison =>
        $"{EstimatedChargingWaitBeforeMinutes}/{EstimatedChargingWaitAfterMinutes}";
}

public sealed record ImpactReport(
    IReadOnlyList<HubImpact> Hubs,
    int AffectedHubCount,
    int OverloadedHubCount,
    int CriticalHubCount,
    string Summary);

public sealed record ForecastPoint(
    DateTime At,
    int OccupiedSlots,
    int Capacity,
    double ParkingUtilization,
    double ChargingUtilization);

public sealed record DispatchRecommendation(
    string Id,
    string Title,
    string Description,
    string Reason,
    RecommendationAction Action,
    string? SourceHubId = null,
    string? DestinationHubId = null,
    int Quantity = 0);

public sealed record SimulationResult(
    string SessionId,
    string RunId,
    string ScenarioName,
    SimulationParameters Parameters,
    DateTime StartedAt,
    TimeSpan Duration,
    NetworkSnapshot BeforeState,
    NetworkSnapshot AfterState,
    ImpactReport Impact,
    IReadOnlyList<DispatchRecommendation> Recommendations,
    bool IsPostActionResult,
    string? AppliedRecommendationId = null)
{
    public IReadOnlyList<ForecastPoint> Forecast { get; init; } = Array.Empty<ForecastPoint>();
}

public sealed class SimulationSession
{
    public required string SessionId { get; init; }
    public required ScenarioPreset Preset { get; init; }
    public required SimulationParameters Parameters { get; init; }
    public required DateTime CreatedAt { get; init; }
    public SimulationState State { get; set; } = SimulationState.Configuring;
    public NetworkSnapshot? OriginalSnapshot { get; set; }
    public List<SimulationResult> Runs { get; } = new();
    public SimulationResult? LatestResult => Runs.LastOrDefault();
}
