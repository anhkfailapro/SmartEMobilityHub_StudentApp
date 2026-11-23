using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

/// <summary>
/// Điều phối xe giữa các Hub lệch tải: phát hiện Hub dư/thiếu xe, gợi ý lệnh, tạo lệnh thủ công,
/// xuất phát và hoàn tất. Mọi lệnh chỉ chứa xe thật, đang sẵn sàng tại Hub nguồn và chưa nằm
/// trong lệnh khác; Hub đích phải còn đủ chỗ.
/// </summary>
public sealed class VehicleRedistributor
{
    private readonly NetworkStore _store;
    private readonly IClock _clock;
    private int _orderSeq;

    public double HighLoadThreshold { get; set; } = ServiceConstants.DispatchHighLoadRatio;
    public double LowLoadThreshold { get; set; } = ServiceConstants.DispatchLowLoadRatio;
    public double TargetLoadRatio { get; set; } = ServiceConstants.DispatchTargetLoadRatio;

    public List<DispatchOrder> SuggestedOrders { get; } = new();

    public VehicleRedistributor(NetworkStore store, IClock? clock = null)
    {
        _store = store;
        _clock = clock ?? new SystemClock();
    }

    public List<MobilityHub> AnalyzeHubImbalance(IEnumerable<MobilityHub>? hubs = null)
    {
        return ActiveHubs(hubs)
            .Where(h => h.ParkingUtilization >= HighLoadThreshold || h.ParkingUtilization <= LowLoadThreshold)
            .OrderByDescending(h => h.ParkingUtilization)
            .ToList();
    }

    /// <summary>Khoảng cách đường chim bay (km) và thời gian ước tính (phút).</summary>
    public Dictionary<string, double> CalculateEstimatedRoute(MobilityHub source, MobilityHub target)
    {
        if (source is null || target is null) throw new IllegalArgumentException("Hub nguồn/đích không hợp lệ.");

        var distanceKm = Math.Round(source.Location.DistanceKmTo(target.Location), 2);
        var durationMinutes = Math.Round(
            Math.Max(ServiceConstants.DispatchMinDurationMinutes, distanceKm / ServiceConstants.DispatchAverageSpeedKmh * 60.0),
            1);

        return new Dictionary<string, double>
        {
            ["distanceKm"] = distanceKm,
            ["durationMinutes"] = durationMinutes
        };
    }

    /// <summary>
    /// Gợi ý lệnh điều phối (chưa ghi vào store). Số xe mỗi lệnh bị chặn bởi: phần dư của Hub cho,
    /// phần thiếu còn lại của Hub nhận (đã trừ các gợi ý trước), và số xe thật đang sẵn sàng.
    /// </summary>
    public List<DispatchOrder> GenerateDispatchSuggestions(IEnumerable<MobilityHub>? hubs = null)
    {
        SuggestedOrders.Clear();
        var activeHubs = ActiveHubs(hubs).ToList();

        var donors = activeHubs
            .Where(h => h.ParkingUtilization >= HighLoadThreshold && h.OccupiedParkingSlots > 1)
            .OrderByDescending(h => h.ParkingUtilization)
            .ToList();

        var receivers = activeHubs
            .Where(h => h.ParkingUtilization <= LowLoadThreshold && h.FreeParkingSlots > 0)
            .OrderBy(h => h.ParkingUtilization)
            .ToList();

        // Phần thiếu còn lại của từng Hub nhận, trừ dần khi đã có gợi ý đổ vào.
        var remainingDeficit = receivers.ToDictionary(
            r => r.Id,
            r => Math.Min(
                FreeSlotsAfterIncoming(r),
                Math.Max(0, (int)Math.Ceiling(r.ParkingCapacity * TargetLoadRatio) - r.OccupiedParkingSlots - IncomingVehicles(r.Id))));

        var reserved = VehiclesInActiveOrders();

        foreach (var donor in donors)
        {
            var surplus = Math.Max(0, donor.OccupiedParkingSlots - (int)Math.Floor(donor.ParkingCapacity * TargetLoadRatio));
            var available = AvailableVehiclesAt(donor.Id, reserved);

            foreach (var receiver in receivers)
            {
                if (surplus == 0 || available.Count == 0) break;

                var moveCount = Math.Min(Math.Min(surplus, remainingDeficit[receiver.Id]), available.Count);
                if (moveCount <= 0) continue;

                var vehicles = available.Take(moveCount).ToList();
                available.RemoveRange(0, moveCount);
                foreach (var code in vehicles) reserved.Add(code);

                var route = CalculateEstimatedRoute(donor, receiver);
                SuggestedOrders.Add(new DispatchOrder
                {
                    OrderId = $"DSP-SUG-{++_orderSeq:000}",
                    SourceHubId = donor.Id,
                    TargetHubId = receiver.Id,
                    VehicleIds = vehicles,
                    Reason = $"Cân bằng tải {donor.ShortName} ({donor.ParkingUtilization:P0}) -> {receiver.ShortName} ({receiver.ParkingUtilization:P0})",
                    EstimatedDistanceKm = route["distanceKm"],
                    EstimatedDurationMinutes = route["durationMinutes"],
                    Status = DispatchOrderStatus.Pending,
                    CreatedAt = _clock.Now
                });

                surplus -= moveCount;
                remainingDeficit[receiver.Id] -= moveCount;
            }
        }

        return SuggestedOrders.ToList();
    }

    public DispatchOrder CreateOrder(
        string sourceHubId,
        string targetHubId,
        IEnumerable<string> vehicleIds,
        string staffId,
        string reason = "Điều phối thủ công từ Operator")
    {
        if (string.IsNullOrWhiteSpace(sourceHubId) || string.IsNullOrWhiteSpace(targetHubId))
            throw new IllegalArgumentException("Hub nguồn và Hub đích không được để trống.");

        if (string.Equals(sourceHubId, targetHubId, StringComparison.OrdinalIgnoreCase))
            throw new IllegalArgumentException("Hub nguồn và Hub đích phải khác nhau.");

        var sourceHub = _store.GetHub(sourceHubId);
        var targetHub = _store.GetHub(targetHubId);
        var vehicles = (vehicleIds ?? Enumerable.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (vehicles.Count == 0)
            throw new IllegalArgumentException("Cần chọn ít nhất 1 phương tiện để điều phối.");

        var reserved = VehiclesInActiveOrders();
        var codes = new List<string>();

        foreach (var id in vehicles)
        {
            var vehicle = _store.FindVehicle(id)
                          ?? throw new IllegalArgumentException($"Không tìm thấy xe {id}.");

            if (vehicle.HubId != sourceHub.Id)
                throw new IllegalArgumentException($"Xe {vehicle.Code} không ở {sourceHub.Id}.");

            if (vehicle.Status != DeviceStatus.Available)
                throw new IllegalArgumentException($"Xe {vehicle.Code} đang {vehicle.Status}, chỉ điều phối được xe sẵn sàng.");

            if (reserved.Contains(vehicle.Code))
                throw new IllegalArgumentException($"Xe {vehicle.Code} đã nằm trong một lệnh điều phối khác.");

            codes.Add(vehicle.Code);
        }

        var freeSlots = FreeSlotsAfterIncoming(targetHub);
        if (codes.Count > freeSlots)
            throw new IllegalArgumentException($"{targetHub.Id} chỉ còn {freeSlots} chỗ trống, không nhận đủ {codes.Count} xe.");

        var route = CalculateEstimatedRoute(sourceHub, targetHub);
        var order = new DispatchOrder
        {
            OrderId = $"DSP-{++_orderSeq:0000}",
            SourceHubId = sourceHub.Id,
            TargetHubId = targetHub.Id,
            VehicleIds = codes,
            StaffId = string.IsNullOrWhiteSpace(staffId) ? "STAFF-01" : staffId.Trim(),
            Reason = reason,
            EstimatedDistanceKm = route["distanceKm"],
            EstimatedDurationMinutes = route["durationMinutes"],
            Status = DispatchOrderStatus.Pending,
            CreatedAt = _clock.Now
        };

        _store.DispatchOrders.Add(order);
        return order;
    }

    /// <summary>Xuất phát: chỉ khi mọi xe vẫn đang sẵn sàng tại Hub nguồn.</summary>
    public bool ExecuteRedistribution(string orderId)
    {
        var order = _store.DispatchOrders.FirstOrDefault(o => o.OrderId == orderId);
        if (order is null || order.Status != DispatchOrderStatus.Pending) return false;

        var vehicles = order.VehicleIds.Select(code => _store.FindVehicle(code)).ToList();
        if (vehicles.Any(v => v is null || v.HubId != order.SourceHubId || v.Status != DeviceStatus.Available))
            return false;

        if (!order.ExecuteRedistribution(_clock.Now)) return false;

        var sourceHub = _store.FindHub(order.SourceHubId);
        if (sourceHub is not null)
            sourceHub.OccupiedParkingSlots = Math.Max(0, sourceHub.OccupiedParkingSlots - vehicles.Count);

        foreach (var vehicle in vehicles)
        {
            vehicle!.Status = DeviceStatus.Relocating;
            vehicle.HubId = null;
        }

        return true;
    }

    /// <summary>Hoàn tất: Hub đích phải còn đủ chỗ, nếu không lệnh giữ trạng thái đang điều phối.</summary>
    public bool CompleteOrder(string orderId)
    {
        var order = _store.DispatchOrders.FirstOrDefault(o => o.OrderId == orderId);
        if (order is null || order.Status != DispatchOrderStatus.InProgress) return false;

        var targetHub = _store.FindHub(order.TargetHubId);
        var moved = order.VehicleIds.Count;
        if (targetHub is null || targetHub.FreeParkingSlots < moved) return false;

        if (!order.CompleteOrder(_clock.Now)) return false;

        targetHub.OccupiedParkingSlots += moved;

        foreach (var code in order.VehicleIds)
        {
            var vehicle = _store.FindVehicle(code);
            if (vehicle is null) continue;
            vehicle.Status = DeviceStatus.Available;
            vehicle.HubId = order.TargetHubId;
        }

        return true;
    }

    /// <summary>Huỷ lệnh chưa xuất phát; xe vẫn ở Hub nguồn nên không cần trả tài nguyên.</summary>
    public bool CancelOrder(string orderId)
    {
        var order = _store.DispatchOrders.FirstOrDefault(o => o.OrderId == orderId);
        return order is not null && order.CancelOrder(_clock.Now);
    }

    private IEnumerable<MobilityHub> ActiveHubs(IEnumerable<MobilityHub>? hubs) =>
        (hubs ?? _store.Hubs)
        .Where(h => h.Status == HubStatus.Online && h.Connection == ConnectionStatus.Online && h.ParkingCapacity > 0);

    private HashSet<string> VehiclesInActiveOrders() =>
        _store.DispatchOrders
            .Where(o => o.IsActive)
            .SelectMany(o => o.VehicleIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private List<string> AvailableVehiclesAt(string hubId, HashSet<string> reserved) =>
        _store.Vehicles
            .Where(v => v.HubId == hubId && v.Status == DeviceStatus.Available && !reserved.Contains(v.Code))
            .OrderBy(v => v.Code, StringComparer.Ordinal)
            .Select(v => v.Code)
            .ToList();

    private int IncomingVehicles(string hubId) =>
        _store.DispatchOrders
            .Where(o => o.IsActive && o.TargetHubId == hubId)
            .Sum(o => o.VehicleIds.Count);

    private int FreeSlotsAfterIncoming(MobilityHub hub) =>
        Math.Max(0, hub.FreeParkingSlots - IncomingVehicles(hub.Id));
}
