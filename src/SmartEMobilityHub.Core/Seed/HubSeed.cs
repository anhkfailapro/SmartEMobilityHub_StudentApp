using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Seed;

/// <summary>
/// 12 Hub của mạng lưới (PLAN §4.1). HUB-08 là hero entity của màn hình Incident.
/// Số chỗ đỗ chiếm dụng là bộ đếm độc lập — không cần tạo 54 đối tượng Vehicle.
/// </summary>
public static class HubSeed
{
    public static List<MobilityHub> Build(DateTime now)
    {
        return new List<MobilityHub>
        {
            Hub("HUB-01", "Hub Ga Metro ĐHQG", "Metro ĐHQG", 10.8797, 106.8090, 60, 40, 42, now, 6),
            Hub("HUB-02", "Hub KTX Khu A", "KTX Khu A", 10.8753, 106.8000, 60, 42, 28, now, 4,
                new[] { (2, DeviceStatus.Faulty, 55.0, now) }),
            Hub("HUB-03", "Hub Nhà học A1", "Nhà học A1", 10.8770, 106.8060, 30, 12, 28, now, 4),
            Hub("HUB-04", "Hub Thư viện Trung tâm", "Thư viện Trung tâm", 10.8760, 106.8030, 40, 26, 42, now, 6,
                new[] { (3, DeviceStatus.Faulty, 68.0, now) }),
            Hub("HUB-05", "Hub Nhà học B", "Nhà học B", 10.8775, 106.8075, 30, 18, 28, now, 4),
            Hub("HUB-06", "Hub Trung tâm Hành chính", "Trung tâm Hành chính", 10.8778, 106.8050, 40, 30, 28, now, 4),
            Hub("HUB-07", "Hub Khu Thể thao", "Khu Thể thao", 10.8745, 106.8065, 30, 12, 28, now, 4),
            // Hero: 54/60 = 90% (UC-SYS-03), 8 cổng: 01,02,03,05,06 CHARGING; 08 AVAILABLE;
            // 07 FAULTY 72°C; 04 OFFLINE với dữ liệu cũ 14 phút (NFR 2.2.3).
            Hub("HUB-08", "Hub KTX Khu B", "KTX Khu B", 10.8728, 106.8025, 60, 54, 56, now, 8,
                new[]
                {
                    (4, DeviceStatus.Offline, 31.0, now.AddMinutes(-14)),
                    (7, DeviceStatus.Faulty, 72.0, now)
                }),
            Hub("HUB-09", "Hub KTX Khu C", "KTX Khu C", 10.8718, 106.8050, 50, 40, 42, now, 6,
                new[] { (2, DeviceStatus.Faulty, 74.0, now) }),
            Hub("HUB-10", "Hub Nhà ăn Trung tâm", "Nhà ăn Trung tâm", 10.8768, 106.8040, 30, 20, 28, now, 4),
            Hub("HUB-11", "Hub Nhà văn hóa SV", "Nhà văn hóa SV", 10.8785, 106.8080, 30, 22, 28, now, 4),
            Hub("HUB-12", "Hub Khu Dịch vụ", "Khu Dịch vụ", 10.8790, 106.8010, 40, 34, 42, now, 6,
                new[]
                {
                    (2, DeviceStatus.Faulty, 55.0, now),
                    (5, DeviceStatus.Faulty, 61.0, now)
                })
        };
    }

    private static MobilityHub Hub(
        string id,
        string name,
        string shortName,
        double lat,
        double lng,
        int capacity,
        int occupied,
        double powerLimitKw,
        DateTime updatedAt,
        int portCount,
        (int Number, DeviceStatus Status, double TemperatureC, DateTime UpdatedAt)[]? overrides = null)
    {
        var hub = new MobilityHub
        {
            Id = id,
            Name = name,
            ShortName = shortName,
            Location = new GeoPoint(lat, lng),
            ParkingCapacity = capacity,
            OccupiedParkingSlots = occupied,
            PowerLimitKw = powerLimitKw,
            Connection = ConnectionStatus.Online,
            Status = HubStatus.Online,
            LastUpdatedAt = updatedAt
        };

        var suffix = id.Replace("HUB-", "");
        for (var number = 1; number <= portCount; number++)
        {
            var status = DeviceStatus.Available;
            var temperature = 32.0;
            var portUpdatedAt = updatedAt;

            if (overrides is not null)
            {
                foreach (var item in overrides)
                {
                    if (item.Number != number) continue;
                    status = item.Status;
                    temperature = item.TemperatureC;
                    portUpdatedAt = item.UpdatedAt;
                    break;
                }
            }

            hub.ChargingPoints.Add(new ChargingPoint
            {
                Id = $"CS-{suffix}-{number:00}",
                HubId = id,
                Number = number,
                Status = status,
                PowerKw = ServiceConstants.PowerPerPortKw,
                TemperatureC = temperature,
                LastUpdatedAt = portUpdatedAt
            });
        }

        return hub;
    }
}
