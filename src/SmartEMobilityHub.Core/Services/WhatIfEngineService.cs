using System.Diagnostics;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Services;

/// <summary>UC-WI-02..04: isolated scenario execution, impact assessment and recommendations.</summary>
public sealed class WhatIfEngineService
{
    public const int MaximumDurationSeconds = 3;
    private const double OverloadThreshold = ServiceConstants.OverloadThreshold;

    private readonly IClock _clock;

    /// <param name="clock">Đồng hồ mô phỏng dùng cho mốc chạy (StartedAt); mặc định là giờ thật (DC-53).</param>
    public WhatIfEngineService(IClock? clock = null) => _clock = clock ?? new SystemClock();

    public ValidationResult ValidateParameters(
        ScenarioPreset preset,
        SimulationParameters parameters,
        NetworkSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (string.IsNullOrWhiteSpace(parameters.TargetHubId) || snapshot.FindHub(parameters.TargetHubId) is null)
            return ValidationResult.Invalid("Hãy chọn một Hub có trong mạng lưới.");
        if (parameters.DemandSurgePercent is < -50 or > 200)
            return ValidationResult.Invalid("Mức biến động nhu cầu phải nằm trong khoảng -50% đến +200%.");
        if (parameters.StartTime >= parameters.EndTime)
            return ValidationResult.Invalid("Thời điểm bắt đầu phải sớm hơn thời điểm kết thúc.");
        if ((parameters.EndTime - parameters.StartTime).TotalHours > 24)
            return ValidationResult.Invalid("Khung mô phỏng không được dài quá 24 giờ.");
        if (parameters.TimeStepMinutes is < 5 or > 120)
            return ValidationResult.Invalid("Bước mô phỏng phải từ 5 đến 120 phút.");

        return preset.ValidateCustomValues(parameters, snapshot);
    }

    public SimulationResult ExecuteSimulation(
        string sessionId,
        string runId,
        ScenarioPreset preset,
        NetworkSnapshot snapshot,
        SimulationParameters parameters,
        bool isPostActionResult = false,
        string? appliedRecommendationId = null)
    {
        var validation = ValidateParameters(preset, parameters, snapshot);
        if (!validation.IsValid) throw new ArgumentException(validation.Error, nameof(parameters));

        var stopwatch = Stopwatch.StartNew();
        var before = new NetworkSnapshot(snapshot.TakenAt, snapshot.Hubs, snapshot.Vehicles,
            snapshot.Reservations, snapshot.Incidents, snapshot.Technicians, snapshot.MaintenanceTasks,
            snapshot.Alerts, snapshot.ChargingRequests, snapshot.Operators);
        var afterHubs = snapshot.Hubs.Select(h => h.Clone()).ToList();
        var afterVehicles = snapshot.Vehicles.Select(v => v.Clone()).ToList();
        var afterRequests = snapshot.ChargingRequests.Select(r => r.Clone()).ToList();
        var originalRequests = snapshot.ChargingRequests.ToList();

        var target = afterHubs.First(h => h.Id == parameters.TargetHubId);
        var beforeTarget = snapshot.FindHub(parameters.TargetHubId)!;
        var stepCount = Math.Max(1, (int)Math.Ceiling((parameters.EndTime - parameters.StartTime).TotalMinutes / parameters.TimeStepMinutes));
        var forecast = new List<ForecastPoint>
        {
            new(parameters.StartTime, beforeTarget.OccupiedParkingSlots, beforeTarget.ParkingCapacity,
                beforeTarget.ParkingUtilization, beforeTarget.ChargingUtilization)
        };

        // Run deterministic, bounded time steps. Every step is applied only to local clones.
        for (var step = 1; step <= stepCount; step++)
        {
            var progress = (double)step / stepCount;
            ApplyScenarioStep(preset.Type, target, beforeTarget, afterHubs, afterVehicles,
                originalRequests, afterRequests, snapshot, parameters, progress);
            var at = parameters.StartTime.AddMinutes(Math.Min(
                (parameters.EndTime - parameters.StartTime).TotalMinutes,
                step * parameters.TimeStepMinutes));
            forecast.Add(new ForecastPoint(at, target.OccupiedParkingSlots, target.ParkingCapacity,
                target.ParkingUtilization, target.ChargingUtilization));
        }

        var after = new NetworkSnapshot(snapshot.TakenAt, afterHubs, afterVehicles,
            snapshot.Reservations, snapshot.Incidents, snapshot.Technicians, snapshot.MaintenanceTasks,
            snapshot.Alerts, afterRequests, snapshot.Operators);
        var impact = EvaluateImpact(before, after, parameters);
        var recommendations = GenerateRecommendations(impact, after, parameters);
        stopwatch.Stop();
        if (stopwatch.Elapsed > TimeSpan.FromSeconds(MaximumDurationSeconds))
            throw new TimeoutException("Mô phỏng vượt quá giới hạn 3 giây và đã bị hủy.");

        return new SimulationResult(sessionId, runId, preset.Name, parameters, _clock.Now,
            stopwatch.Elapsed, before, after, impact, recommendations, isPostActionResult,
            appliedRecommendationId) { Forecast = forecast };
    }

    public SimulationResult ReSimulateWithAction(
        string sessionId,
        string runId,
        SimulationSession session,
        SimulationResult baseline,
        DispatchRecommendation action)
    {
        var simulated = ExecuteSimulation(sessionId, runId, session.Preset, baseline.BeforeState,
            session.Parameters, true, action.Id);
        var before = simulated.BeforeState;
        var hubs = simulated.AfterState.Hubs.Select(h => h.Clone()).ToList();
        var vehicles = simulated.AfterState.Vehicles.Select(v => v.Clone()).ToList();
        var requests = simulated.AfterState.ChargingRequests.Select(r => r.Clone()).ToList();
        var target = hubs.FirstOrDefault(h => h.Id == session.Parameters.TargetHubId)
                     ?? throw new InvalidOperationException("Hub mô phỏng không còn trong snapshot.");
        var forecastParkingReduction = 0;

        switch (action.Action)
        {
            case RecommendationAction.RedirectDemand:
                if (action.DestinationHubId is { } destinationId)
                {
                    var destination = hubs.FirstOrDefault(h => h.Id == destinationId);
                    if (destination is not null)
                    {
                        var safeOccupancy = (int)Math.Floor(target.ParkingCapacity * (OverloadThreshold - 0.01));
                        var reduction = Math.Min(Math.Max(0, target.OccupiedParkingSlots - safeOccupancy),
                            Math.Min(action.Quantity, destination.FreeParkingSlots));
                        target.OccupiedParkingSlots -= reduction;
                        destination.OccupiedParkingSlots += reduction;
                        forecastParkingReduction = reduction;
                    }
                }
                break;

            case RecommendationAction.RedistributeVehicles:
                if (action.DestinationHubId is { } hubId)
                {
                    var destination = hubs.FirstOrDefault(h => h.Id == hubId);
                    if (destination is not null)
                    {
                        var movable = vehicles.Where(v => v.HubId == target.Id &&
                            v.Status == DeviceStatus.Available && v.Connection == ConnectionStatus.Online)
                            .Take(Math.Min(action.Quantity, destination.FreeParkingSlots)).ToList();
                        foreach (var vehicle in movable) vehicle.HubId = destination.Id;
                        var slots = Math.Min(movable.Count, target.OccupiedParkingSlots);
                        target.OccupiedParkingSlots -= slots;
                        destination.OccupiedParkingSlots += slots;
                        forecastParkingReduction = slots;
                    }
                }
                break;

            case RecommendationAction.PrioritizeLowBattery:
                var ordered = requests.Where(r => r.HubId == target.Id)
                    .OrderBy(r => r.BatteryLevel).ThenBy(r => r.NeededAt).ToList();
                for (var i = 0; i < ordered.Count; i++) ordered[i].QueuePosition = i;
                break;
        }

        var after = new NetworkSnapshot(simulated.AfterState.TakenAt, hubs, vehicles,
            simulated.AfterState.Reservations, simulated.AfterState.Incidents,
            simulated.AfterState.Technicians, simulated.AfterState.MaintenanceTasks,
            simulated.AfterState.Alerts, requests, simulated.AfterState.Operators);
        var impact = EvaluateImpact(before, after, session.Parameters);
        var recommendations = GenerateRecommendations(impact, after, session.Parameters);
        var forecast = simulated.Forecast.Select(point =>
        {
            var occupancy = Math.Max(0, point.OccupiedSlots - forecastParkingReduction);
            return point with
            {
                OccupiedSlots = occupancy,
                ParkingUtilization = point.Capacity <= 0 ? 0 : (double)occupancy / point.Capacity
            };
        }).ToList();
        return simulated with
        {
            AfterState = after,
            Impact = impact,
            Recommendations = recommendations,
            Forecast = forecast,
            IsPostActionResult = true,
            AppliedRecommendationId = action.Id
        };
    }

    public ImpactReport EvaluateImpact(NetworkSnapshot before, NetworkSnapshot after, SimulationParameters parameters)
    {
        var impacts = new List<HubImpact>();
        foreach (var beforeHub in before.Hubs)
        {
            var afterHub = after.FindHub(beforeHub.Id);
            if (afterHub is null) continue;

            var beforeVehicles = before.Vehicles.Count(v => v.HubId == beforeHub.Id &&
                v.Status == DeviceStatus.Available && v.Connection == ConnectionStatus.Online);
            var afterVehicles = after.Vehicles.Count(v => v.HubId == afterHub.Id &&
                v.Status == DeviceStatus.Available && v.Connection == ConnectionStatus.Online);
            var beforeBusy = beforeHub.ChargingPoints.Count(p => p.Status == DeviceStatus.Charging);
            var afterBusy = afterHub.ChargingPoints.Count(p => p.Status == DeviceStatus.Charging);
            var beforeWait = EstimateWait(before, beforeHub);
            var afterWait = EstimateWait(after, afterHub);
            var utilization = afterHub.ParkingCapacity <= 0 ? 0 :
                (double)afterHub.OccupiedParkingSlots / afterHub.ParkingCapacity;
            var overallUtilization = Math.Max(utilization, afterHub.ChargingUtilization);

            impacts.Add(new HubImpact(afterHub.Id, afterHub.Name,
                beforeHub.OccupiedParkingSlots, afterHub.OccupiedParkingSlots,
                afterHub.ParkingCapacity, beforeHub.ParkingUtilization, utilization,
                beforeHub.ChargingUtilization, afterHub.ChargingUtilization,
                beforeVehicles, afterVehicles, beforeBusy, afterBusy,
                beforeWait, afterWait, overallUtilization >= OverloadThreshold,
                afterHub.OccupiedParkingSlots > afterHub.ParkingCapacity));
        }

        var target = impacts.FirstOrDefault(h => h.HubId == parameters.TargetHubId);
        var affected = impacts.Count(h => Math.Abs(h.ParkingUtilizationAfter - h.ParkingUtilizationBefore) > 0.0001 ||
            h.ChargingPortsBusyAfter != h.ChargingPortsBusyBefore ||
            h.AvailableVehiclesAfter != h.AvailableVehiclesBefore);
        var overloaded = impacts.Count(h => h.IsOverloaded);
        var critical = impacts.Count(h => h.IsCritical);
        var summary = target is null
            ? "Không có đủ dữ liệu để đánh giá Hub đã chọn."
            : target.IsCritical
                ? $"{target.HubName} dự kiến vượt sức chứa bãi đỗ ({target.ParkingUtilizationAfter:P0}); cần xử lý khẩn cấp."
                : target.IsOverloaded
                    ? $"{target.HubName} dự kiến chạm ngưỡng cảnh báo (tải cao nhất {target.OverallUtilizationAfter:P0})."
                    : $"{target.HubName} dự kiến ở mức tải cao nhất {target.OverallUtilizationAfter:P0}, chưa chạm ngưỡng 90%.";

        return new ImpactReport(impacts, affected, overloaded, critical, summary);
    }

    public IReadOnlyList<DispatchRecommendation> GenerateRecommendations(
        ImpactReport impact,
        NetworkSnapshot snapshot,
        SimulationParameters parameters)
    {
        var target = snapshot.FindHub(parameters.TargetHubId)
                     ?? throw new InvalidOperationException("Không tìm thấy Hub đích.");
        var alternate = snapshot.Hubs.Where(h => h.Id != target.Id && h.Status == HubStatus.Online &&
                h.Connection == ConnectionStatus.Online && h.FreeParkingSlots > 0)
            .OrderByDescending(h => h.FreeParkingSlots)
            .ThenBy(h => DistanceKm(target.Location, h.Location))
            .FirstOrDefault();
        var targetImpact = impact.Hubs.FirstOrDefault(h => h.HubId == target.Id);
        var parkingReduction = targetImpact is null ? 0 : Math.Max(0,
            targetImpact.OccupiedAfter - (int)Math.Floor(target.ParkingCapacity * (OverloadThreshold - 0.01)));
        if (alternate is not null) parkingReduction = Math.Min(parkingReduction, alternate.FreeParkingSlots);
        var movableVehicles = snapshot.Vehicles.Count(v => v.HubId == target.Id &&
            v.Status == DeviceStatus.Available && v.Connection == ConnectionStatus.Online);
        var lowBattery = snapshot.ChargingRequests.Count(r => r.BatteryLevel < 20);

        return new[]
        {
            new DispatchRecommendation("WI-REDIRECT", alternate is null
                    ? "Điều hướng nhu cầu thủ công"
                    : $"Điều hướng khách sang {alternate.ShortName}",
                alternate is null
                    ? "Chọn Hub thay thế thủ công vì các Hub khác chưa có chỗ trống phù hợp."
                    : parkingReduction == 0
                        ? "Hub mục tiêu chưa cần chuyển hướng để giảm tải chỗ đỗ."
                        : $"Phân tán khoảng {parkingReduction} lượt đến Hub {alternate.ShortName}.",
                "Giảm áp lực chỗ đỗ tại Hub mục tiêu và tận dụng sức chứa còn trống.",
                RecommendationAction.RedirectDemand, target.Id, alternate?.Id, parkingReduction),
            new DispatchRecommendation("WI-VEHICLES", alternate is null
                    ? "Rà soát điều phối phương tiện"
                    : $"Điều chuyển xe khả dụng tới {alternate.ShortName}",
                movableVehicles == 0 || alternate is null
                    ? "Không có đủ xe và chỗ trống để điều chuyển tự động; cần Operator chọn phương án thủ công."
                    : $"Di chuyển tối đa {Math.Min(5, movableVehicles)} xe khả dụng, giữ nguyên dữ liệu vận hành.",
                "Cân bằng vị trí xe để giảm tập trung phương tiện tại khu vực đang chịu tải.",
                RecommendationAction.RedistributeVehicles, target.Id, alternate?.Id, Math.Min(5, movableVehicles)),
            new DispatchRecommendation("WI-CHARGE", "Ưu tiên xe pin dưới 20%",
                lowBattery == 0
                    ? "Đưa xe pin thấp lên đầu hàng đợi khi có yêu cầu sạc mới."
                    : $"Ưu tiên {lowBattery} yêu cầu sạc có pin dưới 20% trước các yêu cầu còn lại.",
                "Giảm nguy cơ phương tiện hết pin trong khung giờ cao điểm.",
                RecommendationAction.PrioritizeLowBattery, target.Id, null, lowBattery)
        };
    }

    private static void ApplyScenarioStep(ScenarioType type, MobilityHub target, MobilityHub original,
        List<MobilityHub> hubs, List<Vehicle> vehicles,
        List<ChargingRequest> originalRequests, List<ChargingRequest> requests,
        NetworkSnapshot baseline, SimulationParameters parameters, double progress)
    {
        var percentage = parameters.DemandSurgePercent / 100d;
        switch (type)
        {
            case ScenarioType.MetroDemandSurge:
                target.OccupiedParkingSlots = Math.Max(0, original.OccupiedParkingSlots +
                    (int)Math.Ceiling(original.ParkingCapacity * percentage * progress));
                break;
            case ScenarioType.ParkingFull:
                target.OccupiedParkingSlots = Math.Max(0, original.ParkingCapacity +
                    (int)Math.Ceiling(original.ParkingCapacity * Math.Max(0, percentage) * progress));
                break;
            case ScenarioType.ChargingDemandSurge:
                var extraBusy = (int)Math.Ceiling(original.UsableChargingPorts * Math.Max(0, percentage) * progress);
                var freePorts = target.ChargingPoints.Where(p => p.Status == DeviceStatus.Available)
                    .OrderBy(p => p.Number).ToList();
                foreach (var port in freePorts.Take(extraBusy)) port.Status = DeviceStatus.Charging;
                var desiredExtraRequests = (int)Math.Ceiling(original.UsableChargingPorts *
                    Math.Max(0, percentage) * progress);
                var simulatedPrefix = $"SIM-WI-{target.Id}-";
                var currentExtraRequests = requests.Count(r => r.RequestId.StartsWith(simulatedPrefix, StringComparison.Ordinal));
                for (var i = currentExtraRequests; i < desiredExtraRequests; i++)
                {
                    requests.Add(new ChargingRequest
                    {
                        RequestId = $"{simulatedPrefix}{i + 1:00}",
                        VehicleId = $"PREDICTED-{target.Id}-{i + 1:00}",
                        HubId = target.Id,
                        BatteryLevel = Math.Max(5, 15 - i * 3),
                        NeededAt = parameters.EndTime,
                        PeakDemand = ServiceConstants.DefaultPeakDemand,
                        QueuePosition = originalRequests.Count(r => r.HubId == target.Id) + i
                    });
                }
                break;
            case ScenarioType.ChargerOutage:
                var failedPorts = target.ChargingPoints.Where(p => original.ChargingPoints.Any(op =>
                             op.Id == p.Id && op.Status is not DeviceStatus.Offline and not DeviceStatus.Faulty))
                         .OrderByDescending(p => p.Number).Take(parameters.DisabledChargerCount).ToList();
                foreach (var port in failedPorts)
                {
                    port.Status = DeviceStatus.Offline;
                    if (port.AssignedVehicleCode is { } vehicleCode)
                    {
                        var vehicle = vehicles.FirstOrDefault(v => v.Code == vehicleCode);
                        if (vehicle is not null)
                        {
                            vehicle.Status = DeviceStatus.NeedsCharge;
                            vehicle.AssignedPortId = null;
                        }
                    }
                }
                break;
            case ScenarioType.VehicleConcentration:
                var source = baseline.Vehicles.Where(v => v.HubId != target.Id && v.Status == DeviceStatus.Available &&
                        v.Connection == ConnectionStatus.Online)
                    .GroupBy(v => v.HubId).OrderByDescending(g => g.Count()).FirstOrDefault();
                if (source is not null)
                {
                    if (source.Key is not { } sourceHubId) break;
                    var sourceVehicles = source.OrderBy(v => v.Code).ToList();
                    var requested = (int)Math.Ceiling(sourceVehicles.Count * Math.Max(0, percentage) * progress);
                    var free = Math.Max(0, target.ParkingCapacity - original.OccupiedParkingSlots);
                    var movedCount = Math.Min(requested, Math.Min(sourceVehicles.Count, free));
                    var movedCodes = sourceVehicles.Take(movedCount).Select(v => v.Code)
                        .ToHashSet(StringComparer.Ordinal);
                    foreach (var baselineVehicle in sourceVehicles)
                    {
                        var simulatedVehicle = vehicles.FirstOrDefault(v => v.Code == baselineVehicle.Code);
                        if (simulatedVehicle is not null)
                        simulatedVehicle.HubId = movedCodes.Contains(baselineVehicle.Code)
                                ? target.Id : sourceHubId;
                    }
                    target.OccupiedParkingSlots = original.OccupiedParkingSlots + movedCount;
                    var sourceHub = hubs.FirstOrDefault(h => h.Id == sourceHubId);
                    if (sourceHub is not null)
                    {
                        var baselineSource = baseline.FindHub(sourceHubId);
                        sourceHub.OccupiedParkingSlots = Math.Max(0,
                            (baselineSource?.OccupiedParkingSlots ?? sourceHub.OccupiedParkingSlots) - movedCount);
                    }
                }
                break;
        }

        if (type == ScenarioType.ChargingDemandSurge)
        {
            var hubRequests = requests.Where(r => r.HubId == target.Id)
                .OrderBy(r => r.QueuePosition).ToList();
            for (var i = 0; i < hubRequests.Count; i++) hubRequests[i].QueuePosition = i;
        }
    }

    private static int EstimateWait(NetworkSnapshot snapshot, MobilityHub hub)
    {
        var queue = snapshot.ChargingRequests.Where(r => r.HubId == hub.Id && r.AssignedPortId is null)
            .OrderBy(r => r.QueuePosition).ToList();
        if (queue.Count == 0) return 0;
        var lowBattery = queue.Where(r => r.BatteryLevel < 20).ToList();
        var measured = lowBattery.Count == 0 ? queue : lowBattery;
        var availablePorts = hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Available);
        if (availablePorts == 0) availablePorts = 1;
        return (int)Math.Round(measured.Average(r =>
            Math.Ceiling((r.QueuePosition + 1d) / availablePorts) * 30));
    }

    private static double DistanceKm(GeoPoint a, GeoPoint b)
    {
        const double radius = 6371;
        var dLat = Degrees(b.Lat - a.Lat);
        var dLon = Degrees(b.Lng - a.Lng);
        var value = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(Degrees(a.Lat)) * Math.Cos(Degrees(b.Lat)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * radius * Math.Asin(Math.Min(1, Math.Sqrt(value)));
    }

    private static double Degrees(double value) => value * Math.PI / 180;
}
