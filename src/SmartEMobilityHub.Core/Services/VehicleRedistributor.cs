using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

public sealed class VehicleRedistributor
{
    private readonly NetworkStore _store;
    private readonly IClock _clock;
    private int _orderSeq;

    public double HighLoadThreshold { get; set; } = 0.85;
    public double LowLoadThreshold { get; set; } = 0.35;
    public double TargetLoadRatio { get; set; } = 0.65;

    public List<DispatchOrder> SuggestedOrders { get; } = new();

    public VehicleRedistributor(NetworkStore store, IClock? clock = null)
    {
        _store = store;
        _clock = clock ?? new SystemClock();
    }

    public List<MobilityHub> AnalyzeHubImbalance(IEnumerable<MobilityHub>? hubs = null)
    {
        var source = hubs ?? _store.Hubs;
        return source
            .Where(h => h.Status == HubStatus.Online && h.Connection == ConnectionStatus.Online && h.ParkingCapacity > 0)
            .Where(h => h.ParkingUtilization >= HighLoadThreshold || h.ParkingUtilization <= LowLoadThreshold)
            .OrderByDescending(h => h.ParkingUtilization)
            .ToList();
    }

    public Dictionary<string, double> CalculateEstimatedRoute(MobilityHub source, MobilityHub target)
    {
        // Khoảng cách Haversine (km) và thời gian ước tính (phút) ở tốc độ trung bình 25 km/h
        const double earthRadiusKm = 6371.0;
        double ToRad(double deg) => deg * Math.PI / 180.0;

        var dLat = ToRad(target.Location.Lat - source.Location.Lat);
        var dLon = ToRad(target.Location.Lng - source.Location.Lng);
        var lat1 = ToRad(source.Location.Lat);
        var lat2 = ToRad(target.Location.Lat);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distanceKm = Math.Round(earthRadiusKm * c, 2);
        var durationMinutes = Math.Round(Math.Max(3.0, (distanceKm / 25.0) * 60.0), 1);

        return new Dictionary<string, double>
        {
            ["distanceKm"] = distanceKm,
            ["durationMinutes"] = durationMinutes
        };
    }

    public List<DispatchOrder> GenerateDispatchSuggestions(IEnumerable<MobilityHub>? hubs = null)
    {
        SuggestedOrders.Clear();
        var activeHubs = (hubs ?? _store.Hubs)
            .Where(h => h.Status == HubStatus.Online && h.Connection == ConnectionStatus.Online && h.ParkingCapacity > 0)
            .ToList();

        // Hub dư xe (đỗ đầy cao / nhiều xe tập trung) -> chuyển sang Hub thiếu xe (tỷ lệ thấp, còn nhiều chỗ trống)
        var donors = activeHubs
            .Where(h => h.ParkingUtilization >= HighLoadThreshold && h.OccupiedParkingSlots > 1)
            .OrderByDescending(h => h.ParkingUtilization)
            .ToList();

        var receivers = activeHubs
            .Where(h => h.ParkingUtilization <= LowLoadThreshold && h.FreeParkingSlots > 0)
            .OrderBy(h => h.ParkingUtilization)
            .ToList();

        foreach (var donor in donors)
        {
            var surplus = Math.Max(0, donor.OccupiedParkingSlots - (int)Math.Floor(donor.ParkingCapacity * TargetLoadRatio));
            if (surplus == 0) continue;

            foreach (var receiver in receivers)
            {
                var deficit = Math.Min(
                    receiver.FreeParkingSlots,
                    Math.Max(0, (int)Math.Ceiling(receiver.ParkingCapacity * TargetLoadRatio) - receiver.OccupiedParkingSlots));

                var moveCount = Math.Min(surplus, deficit);
                if (moveCount <= 0) continue;

                var route = CalculateEstimatedRoute(donor, receiver);
                var candidateVehicles = _store.Vehicles
                    .Where(v => v.HubId == donor.Id && v.Status == DeviceStatus.Available)
                    .Take(moveCount)
                    .Select(v => v.Code)
                    .ToList();

                // Nếu seed không gắn HubId trực tiếp lên Vehicle thì tạo mã xe giữ chỗ tượng trưng theo số lượng
                while (candidateVehicles.Count < moveCount)
                    candidateVehicles.Add($"{donor.Id}-V{candidateVehicles.Count + 1}");

                SuggestedOrders.Add(new DispatchOrder
                {
                    OrderId = $"DSP-SUG-{++_orderSeq:000}",
                    SourceHubId = donor.Id,
                    TargetHubId = receiver.Id,
                    VehicleIds = candidateVehicles,
                    Reason = $"Cân bằng tải {donor.ShortName} ({donor.ParkingUtilization:P0}) -> {receiver.ShortName} ({receiver.ParkingUtilization:P0})",
                    EstimatedDistanceKm = route["distanceKm"],
                    EstimatedDurationMinutes = route["durationMinutes"],
                    Status = DispatchOrderStatus.Pending,
                    CreatedAt = _clock.Now
                });

                surplus -= moveCount;
                if (surplus == 0) break;
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
        var vehicles = vehicleIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

        if (vehicles.Count == 0)
            throw new IllegalArgumentException("Cần chọn ít nhất 1 phương tiện để điều phối.");

        var route = CalculateEstimatedRoute(sourceHub, targetHub);
        var order = new DispatchOrder
        {
            OrderId = $"DSP-{++_orderSeq:0000}",
            SourceHubId = sourceHub.Id,
            TargetHubId = targetHub.Id,
            VehicleIds = vehicles,
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

    public bool ExecuteRedistribution(string orderId)
    {
        var order = _store.DispatchOrders.FirstOrDefault(o => o.OrderId == orderId);
        if (order is null || !order.ExecuteRedistribution(_clock.Now))
            return false;

        var sourceHub = _store.FindHub(order.SourceHubId);
        var moved = order.VehicleIds.Count;

        if (sourceHub is not null)
            sourceHub.OccupiedParkingSlots = Math.Max(0, sourceHub.OccupiedParkingSlots - moved);

        foreach (var vCode in order.VehicleIds)
        {
            var vehicle = _store.FindVehicle(vCode);
            if (vehicle is not null)
            {
                vehicle.Status = DeviceStatus.Relocating;
                vehicle.HubId = null;
            }
        }

        return true;
    }

    public bool CompleteOrder(string orderId)
    {
        var order = _store.DispatchOrders.FirstOrDefault(o => o.OrderId == orderId);
        if (order is null || !order.CompleteOrder(_clock.Now))
            return false;

        var targetHub = _store.FindHub(order.TargetHubId);
        var moved = order.VehicleIds.Count;

        if (targetHub is not null)
            targetHub.OccupiedParkingSlots = Math.Min(targetHub.ParkingCapacity, targetHub.OccupiedParkingSlots + moved);

        foreach (var vCode in order.VehicleIds)
        {
            var vehicle = _store.FindVehicle(vCode);
            if (vehicle is not null)
            {
                vehicle.Status = DeviceStatus.Available;
                vehicle.HubId = order.TargetHubId;
            }
        }

        return true;
    }
}