using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Seed;

namespace SmartEMobilityHub.Tests;

/// <summary>
/// Kiểm tra nền dữ liệu của GĐ1: hero entities, phân bố 24 sự cố,
/// snapshot deep clone và các cổng sạc của HUB-08 khớp Sub 2 §6.2.
/// </summary>
public static class SeedSmokeTests
{
    public static void Register(TestRunner runner)
    {
        var now = new DateTime(2026, 11, 21, 9, 41, 0);
        var store = NetworkSeed.CreateStore(now);

        runner.Add("SMOKE-01", "Seed đủ 12 Hub", () =>
        {
            Assert.Equal(12, store.Hubs.Count);
            Assert.Equal(12, store.Hubs.Select(h => h.Id).Distinct().Count(), "Mã Hub không được trùng");
        });

        runner.Add("SMOKE-02", "HUB-08 đúng 54/60 và 8 cổng theo Figma", () =>
        {
            var hub = store.GetHub("HUB-08");
            Assert.Equal(60, hub.ParkingCapacity);
            Assert.Equal(54, hub.OccupiedParkingSlots);
            Assert.Equal(6, hub.FreeParkingSlots);
            Assert.Equal(8, hub.ChargingPoints.Count);

            foreach (var number in new[] { 1, 2, 3, 5, 6 })
                Assert.Equal(DeviceStatus.Charging, hub.ChargingPoints[number - 1].Status, $"Cổng {number:00} phải đang sạc");

            Assert.Equal(DeviceStatus.Available, hub.ChargingPoints[7].Status, "Cổng 08 phải sẵn sàng");

            var faulty = hub.ChargingPoints[6];
            Assert.Equal(DeviceStatus.Faulty, faulty.Status, "Cổng 07 phải FAULTY");
            Assert.Equal(72.0, faulty.TemperatureC, "Cổng 07 phải 72°C");

            var offline = hub.ChargingPoints[3];
            Assert.Equal(DeviceStatus.Offline, offline.Status, "Cổng 04 phải OFFLINE");
            Assert.Equal(now.AddMinutes(-14), offline.LastUpdatedAt, "Cổng 04 phải cũ 14 phút");
        });

        runner.Add("SMOKE-03", "Seed đúng 24 sự cố (12 mới / 7 kiểm tra / 5 sửa chữa)", () =>
        {
            Assert.Equal(24, store.Incidents.Count);
            Assert.Equal(12, store.Incidents.Count(i => i.Status == IncidentStatus.New));
            Assert.Equal(7, store.Incidents.Count(i => i.Status == IncidentStatus.UnderCheck));
            Assert.Equal(5, store.Incidents.Count(i => i.Status == IncidentStatus.InRepair));

            var newOnes = store.Incidents.Where(i => i.Status == IncidentStatus.New).ToList();
            Assert.Equal(8, newOnes.Count(i => i.Source == IncidentSource.StudentReport), "8 báo cáo sinh viên");
            Assert.Equal(4, newOnes.Count(i => i.Source == IncidentSource.IotSensor), "4 cảnh báo IoT");

            var underCheck = store.Incidents.Where(i => i.Status == IncidentStatus.UnderCheck).ToList();
            Assert.Equal(5, underCheck.Count(i => i.ObjectType == IncidentObjectType.Vehicle), "5 xe đang kiểm tra");
            Assert.Equal(2, underCheck.Count(i => i.ObjectType == IncidentObjectType.ChargingPoint), "2 cổng đang kiểm tra");
        });

        runner.Add("SMOKE-04", "Đủ 7 mã sự cố hero", () =>
        {
            var heroes = new[] { "SC-1011", "SC-1014", "SC-1017", "SC-1019", "SC-1022", "SC-1023", "SC-1024" };
            foreach (var code in heroes) Assert.True(store.FindIncident(code) is not null, $"Thiếu {code}");
            Assert.Equal(PriorityLevel.Critical, store.GetIncident("SC-1024").Priority, "SC-1024 phải CRITICAL");
        });

        runner.Add("SMOKE-05", "Hero vehicle/technician/reservation", () =>
        {
            var xe118 = store.FindVehicle("XE-118");
            Assert.True(xe118 is not null, "Thiếu XE-118");
            Assert.Equal(18, xe118!.BatteryLevel);
            Assert.Equal(DeviceStatus.NeedsCharge, xe118.Status);

            var t02 = store.Technicians.FirstOrDefault(t => t.TechId == "T-02");
            Assert.True(t02 is not null, "Thiếu T-02");
            Assert.True(t02!.IsAvailable(), "T-02 phải sẵn sàng");
            Assert.InRange(t02.DistanceToHub(store.GetHub("HUB-08")), 0.7, 0.9, "T-02 phải cách HUB-08 khoảng 0,8 km");

            var t03 = store.Technicians.First(t => t.TechId == "T-03");
            Assert.False(t03.IsAvailable(), "T-03 phải đang bận");

            Assert.True(store.Reservations.Any(r => r.Id == "R-101"), "Thiếu R-101");
            Assert.True(store.Reservations.Any(r => r.Id == "R-102"), "Thiếu R-102");
            Assert.True(store.Reservations.Any(r => r.Id == "R-103" && r.IsPickingUp), "R-103 phải isPickingUp");

            var upcoming = store.Reservations.Count(r =>
                r.HubId == "HUB-08" && r.Status == ReservationStatus.PendingPickup && r.StartsAt > now);
            Assert.Equal(5, upcoming, "HUB-08 phải có 5 reservation sắp tới");
        });

        runner.Add("SMOKE-06", "Snapshot là bản sao sâu, không ghi ngược store", () =>
        {
            var snapshot = store.Snapshot();

            snapshot.FindVehicle("XE-118")!.BatteryLevel = 99;
            Assert.Equal(18, store.FindVehicle("XE-118")!.BatteryLevel, "Sửa snapshot không được đổi store");

            snapshot.FindHub("HUB-08")!.ChargingPoints[6].Status = DeviceStatus.Available;
            Assert.Equal(DeviceStatus.Faulty, store.GetHub("HUB-08").ChargingPoints[6].Status, "Cổng trong snapshot phải độc lập");

            snapshot.Incidents[0].Status = IncidentStatus.Closed;
            Assert.Equal(IncidentStatus.New, store.Incidents[0].Status, "Sự cố trong snapshot phải độc lập");
            Assert.Equal(now, snapshot.TakenAt);
        });

        runner.Add("SMOKE-07", "FakeClock tua thời gian được", () =>
        {
            var clock = new FakeClock(now);
            clock.AdvanceMinutes(16);
            Assert.Equal(now.AddMinutes(16), clock.Now);
            clock.AdvanceSeconds(-30);
            Assert.Equal(now.AddMinutes(15).AddSeconds(30), clock.Now);
        });
    }
}
