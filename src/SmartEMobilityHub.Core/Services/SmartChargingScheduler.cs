using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// UC-SYS-01 / UC-OP-04 — lập lịch sạc thông minh (Sub3 Bảng 36/37).
/// Trọng số mặc định 0.50 / 0.35 / 0.15; điểm = 100·(wB·(1−pin/100) + wN·urgency + wP·peak).
/// </summary>
public sealed class SmartChargingScheduler : ISmartSchedulingService
{
    public const double DefaultWeightBattery = 0.50;
    public const double DefaultWeightNeedTime = 0.35;
    public const double DefaultWeightPeak = 0.15;

    private readonly NetworkStore _store;
    private readonly IIoTSensorGateway _gateway;
    private readonly IClock _clock;
    private readonly IAlertSink _alertSink;
    private readonly ISystemLogger _logger;

    private double _weightBattery = DefaultWeightBattery;
    private double _weightNeedTime = DefaultWeightNeedTime;
    private double _weightPeak = DefaultWeightPeak;

    /// <summary>Hub đang có kế hoạch vượt công suất đã báo — tránh báo lại mỗi chu kỳ.</summary>
    private readonly HashSet<string> _overPowerHubs = new();

    public SmartChargingScheduler(
        NetworkStore store,
        IIoTSensorGateway gateway,
        IClock clock,
        IAlertSink alertSink,
        ISystemLogger logger)
    {
        _store = store;
        _gateway = gateway;
        _clock = clock;
        _alertSink = alertSink;
        _logger = logger;
    }

    public double WeightBattery => _weightBattery;
    public double WeightNeedTime => _weightNeedTime;
    public double WeightPeak => _weightPeak;

    /// <summary>Bước 4 của timer — lập lịch cho một Hub.</summary>
    public void TriggerFor(MobilityHub hub) => RunSchedule(hub);

    /// <summary>Bảng 37 — lập kế hoạch đầy đủ (kể cả khi vượt công suất) rồi cấp cổng trong ngân sách.</summary>
    public ChargingPlan RunSchedule(MobilityHub hub)
    {
        if (hub is null) throw new IllegalArgumentException("Hub không hợp lệ.");

        var queue = _store.ChargingRequests
            .Where(r => r.HubId == hub.Id && r.AssignedPortId is null)
            .ToList();

        foreach (var request in queue)
        {
            if (!request.Pinned) request.PriorityScore = CalculatePriorityScore(request);
        }

        var ordered = queue
            .OrderByDescending(r => r.PriorityScore)
            .ThenBy(r => r.NeededAt)
            .ThenBy(r => r.RequestId, StringComparer.Ordinal)
            .ToList();

        // Ghim thủ công phải thắng điểm số: chèn lại đúng vị trí sau khi xếp hạng.
        foreach (var pinned in queue.Where(r => r.Pinned).OrderBy(r => r.QueuePosition))
        {
            ordered.Remove(pinned);
            ordered.Insert(Math.Clamp(pinned.QueuePosition, 0, ordered.Count), pinned);
        }

        var plan = new ChargingPlan
        {
            HubId = hub.Id,
            TotalPowerKw = ordered.Count * ServiceConstants.PowerPerPortKw
        };

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].QueuePosition = index;
            plan.OrderedRequests.Add(ordered[index]);
        }

        if (CheckPowerLimit(hub, plan))
        {
            _overPowerHubs.Remove(hub.Id);
        }
        else if (_overPowerHubs.Add(hub.Id))
        {
            var message = $"{hub.ShortName}: đang sạc {UsedPowerKw(hub):0.#} kW + kế hoạch cần {plan.TotalPowerKw:0.#} kW, vượt giới hạn {hub.PowerLimitKw:0.#} kW. " +
                          "Đề xuất giãn tiến độ sạc các yêu cầu cuối hàng chờ.";
            _logger.Warn(message, "UC-OP-04");
            _alertSink.DispatchAlert(new Alert
            {
                AlertId = _store.NextAlertId(),
                Type = AlertType.PowerLimit,
                HubId = hub.Id,
                Message = message,
                CreatedAt = _clock.Now
            });
        }

        AllocatePower(hub, plan);
        return plan;
    }

    /// <summary>Bảng 37 — điểm ưu tiên; ném IllegalArgumentException với dữ liệu không hợp lệ.</summary>
    public double CalculatePriorityScore(ChargingRequest request)
    {
        if (request is null) throw new IllegalArgumentException("Yêu cầu sạc không hợp lệ.");
        if (request.BatteryLevel < 0 || request.BatteryLevel > 100)
            throw new IllegalArgumentException($"Mức pin phải trong 0–100%, nhận được {request.BatteryLevel}.");

        var minutesLeft = (request.NeededAt - _clock.Now).TotalMinutes;
        var urgency = Math.Clamp(1 - minutesLeft / ServiceConstants.UrgencyWindowMinutes, 0, 1);

        var score = 100 * (
            _weightBattery * (1 - request.BatteryLevel / 100.0) +
            _weightNeedTime * urgency +
            _weightPeak * request.PeakDemand);

        request.PriorityScore = score;
        return score;
    }

    /// <summary>Bảng 37 — đổi trọng số; sai thì giữ nguyên cấu hình cũ.</summary>
    public void UpdatePriorityRules(double weightBattery, double weightNeedTime, double weightPeak)
    {
        if (weightBattery < 0 || weightBattery > 1 ||
            weightNeedTime < 0 || weightNeedTime > 1 ||
            weightPeak < 0 || weightPeak > 1)
        {
            throw new IllegalArgumentException("Mỗi trọng số phải nằm trong [0, 1].");
        }

        if (Math.Abs(weightBattery + weightNeedTime + weightPeak - 1) > 1e-6)
            throw new IllegalArgumentException("Tổng ba trọng số phải bằng 1.");

        _weightBattery = weightBattery;
        _weightNeedTime = weightNeedTime;
        _weightPeak = weightPeak;
    }

    /// <summary>Bảng 37 — Operator ghim thứ tự; yêu cầu bị ghim không bị tính lại điểm.</summary>
    public bool ManualReorder(string requestId, int newPosition)
    {
        var request = _store.ChargingRequests.FirstOrDefault(r => r.RequestId == requestId);
        if (request is null) return false;

        var queue = _store.ChargingRequests
            .Where(r => r.HubId == request.HubId && r.AssignedPortId is null)
            .OrderBy(r => r.QueuePosition)
            .ToList();

        if (newPosition < 0 || newPosition >= queue.Count) return false;

        queue.Remove(request);
        queue.Insert(newPosition, request);
        for (var index = 0; index < queue.Count; index++) queue[index].QueuePosition = index;

        request.Pinned = true;
        return true;
    }

    /// <summary>
    /// Bảng 37 — so công suất đang sạc + công suất kế hoạch với giới hạn Hub
    /// (cùng ngân sách mà <see cref="AllocatePower"/> dùng để cấp cổng).
    /// </summary>
    public bool CheckPowerLimit(MobilityHub hub, ChargingPlan plan)
    {
        if (hub is null || plan is null) throw new IllegalArgumentException("Hub hoặc kế hoạch không hợp lệ.");
        return UsedPowerKw(hub) + plan.TotalPowerKw <= hub.PowerLimitKw + 1e-9;
    }

    private static double UsedPowerKw(MobilityHub hub) =>
        hub.ChargingPoints.Where(p => p.Status == DeviceStatus.Charging).Sum(p => p.PowerKw);

    /// <summary>
    /// Bảng 37 — cấp cổng theo thứ tự ưu tiên trong ngân sách công suất còn lại.
    /// Trả về số cổng đã cấp thành công.
    /// </summary>
    public int AllocatePower(MobilityHub hub, ChargingPlan plan)
    {
        if (hub is null || plan is null) throw new IllegalArgumentException("Hub hoặc kế hoạch không hợp lệ.");

        var budgetKw = hub.PowerLimitKw - UsedPowerKw(hub);
        var freePorts = hub.ChargingPoints
            .Where(p => p.Status == DeviceStatus.Available)
            .OrderBy(p => p.Number)
            .ToList();

        var allocated = 0;

        foreach (var request in plan.OrderedRequests)
        {
            if (request.AssignedPortId is not null) continue;

            while (request.AssignedPortId is null && budgetKw + 1e-9 >= ServiceConstants.PowerPerPortKw)
            {
                var point = freePorts.FirstOrDefault();
                if (point is null) break;

                var result = _gateway.SendPowerCommand(point.Id, point.PowerKw);
                freePorts.Remove(point);

                if (result.Outcome == PowerCommandOutcome.Timeout)
                {
                    // Mất phản hồi tạm thời: bỏ qua cổng ở chu kỳ này, không coi là hỏng (DC-43).
                    _logger.Warn($"Cổng {point.Id} không phản hồi lệnh cấp nguồn — thử lại ở chu kỳ sau.", "UC-SYS-01");
                    continue;
                }

                if (result.Outcome != PowerCommandOutcome.Ack)
                {
                    MarkPortFaulty(point, result.Message);
                    continue; // thử cổng kế tiếp cho cùng yêu cầu
                }

                point.Status = DeviceStatus.Charging;
                point.AssignedVehicleCode = request.VehicleId;
                point.TemperatureC = Math.Max(point.TemperatureC, 36);
                request.AssignedPortId = point.Id;

                var vehicle = _store.FindVehicle(request.VehicleId);
                if (vehicle is not null)
                {
                    vehicle.Status = DeviceStatus.Charging;
                    vehicle.AssignedPortId = point.Id;
                    vehicle.LastUpdatedAt = _clock.Now;
                }

                budgetKw -= point.PowerKw;
                allocated++;
                break;
            }
        }

        return allocated;
    }

    /// <summary>
    /// Bảng 37 — cổng đang cấp nguồn bị lỗi: đánh dấu FAULTY, báo Operator, chuyển xe sang cổng
    /// trống; không còn cổng thì đưa yêu cầu lên đầu hàng chờ và trả false (UC-SYS-01 Exception 5a).
    /// </summary>
    public bool HandleFaultyChargingPoint(ChargingPoint point)
    {
        if (point is null) throw new IllegalArgumentException("Cổng sạc không hợp lệ.");

        var vehicleCode = point.AssignedVehicleCode;
        var hub = _store.GetHub(point.HubId);
        var request = _store.ChargingRequests.FirstOrDefault(r => r.AssignedPortId == point.Id);

        MarkPortFaulty(point, "Cổng gặp lỗi khi đang cấp nguồn.");

        if (string.IsNullOrEmpty(vehicleCode)) return false;

        var vehicle = _store.FindVehicle(vehicleCode);
        if (request is not null) request.AssignedPortId = null;

        var alternative = hub.ChargingPoints
            .Where(p => p.Status == DeviceStatus.Available)
            .OrderBy(p => p.Number)
            .FirstOrDefault();

        if (alternative is null)
        {
            if (vehicle is not null)
            {
                vehicle.Status = DeviceStatus.NeedsCharge;
                vehicle.AssignedPortId = null;
            }

            if (request is not null) MoveToFrontOfQueue(request);
            return false;
        }

        alternative.Status = DeviceStatus.Charging;
        alternative.AssignedVehicleCode = vehicleCode;
        alternative.TemperatureC = Math.Max(alternative.TemperatureC, 36);

        if (vehicle is not null)
        {
            vehicle.Status = DeviceStatus.Charging;
            vehicle.AssignedPortId = alternative.Id;
        }

        if (request is not null) request.AssignedPortId = alternative.Id;
        return true;
    }

    /// <summary>Bảng 37 — hubId không tồn tại trả danh sách rỗng.</summary>
    public List<ChargingRequest> GetQueue(string hubId)
    {
        return _store.ChargingRequests
            .Where(r => r.HubId == hubId)
            .OrderBy(r => r.QueuePosition)
            .ThenBy(r => r.RequestId, StringComparer.Ordinal)
            .ToList();
    }

    private void MarkPortFaulty(ChargingPoint point, string reason)
    {
        point.Status = DeviceStatus.Faulty;
        point.AssignedVehicleCode = null;

        _logger.Error($"Cổng {point.Id} lỗi: {reason}", "UC-SYS-01");
        _alertSink.DispatchAlert(new Alert
        {
            AlertId = _store.NextAlertId(),
            Type = AlertType.ChargerFault,
            HubId = point.HubId,
            Message = $"Cổng {point.Id} tại {point.HubId} gặp lỗi: {reason}",
            CreatedAt = _clock.Now
        });
    }

    private void MoveToFrontOfQueue(ChargingRequest request)
    {
        var others = _store.ChargingRequests
            .Where(r => r.HubId == request.HubId && r.AssignedPortId is null && r.RequestId != request.RequestId)
            .OrderBy(r => r.QueuePosition)
            .ToList();

        // Ghim để chu kỳ lập lịch kế tiếp không xếp lại theo điểm và đẩy xe xuống (Exception 5a).
        request.Pinned = true;
        request.QueuePosition = 0;
        for (var index = 0; index < others.Count; index++) others[index].QueuePosition = index + 1;
    }
}
