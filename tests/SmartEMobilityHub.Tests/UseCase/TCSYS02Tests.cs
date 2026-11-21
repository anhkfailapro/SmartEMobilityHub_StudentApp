using SmartEMobilityHub.Core.Adapters;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.UseCase;

/// <summary>UC-SYS-02 — Bảng 63 (TCSYS02-01..03).</summary>
public static class TCSYS02Tests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("TCSYS02-01", "Đồng bộ trạng thái Hub theo chu kỳ 30 giây", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-01");

            Assert.Equal(40, hub.OccupiedParkingSlots, "HUB-01 có 40/60 chỗ đã dùng");
            Assert.Equal(20, hub.FreeParkingSlots, "Còn 20 chỗ trống");

            world.Timer.TickNow();

            Assert.Equal(world.Clock.Now, hub.LastUpdatedAt, "lastUpdatedAt phải bằng thời điểm tick");
            Assert.Equal(6, hub.ChargingPoints.Count, "HUB-01 có 6 cổng sạc");
            Assert.Equal(ConnectionStatus.Online, hub.Connection);
            Assert.Equal(30_000, StatusUpdaterTimer.ScanIntervalMs, "Chu kỳ quét 30 giây");
            Assert.Equal(30, world.Timer.GetNextTickInSeconds(), "Chu kỳ giữa hai tick là 30 giây");
        });

        runner.Add("TCSYS02-02", "Mất kết nối cổng CS-08-04 không chặn Hub khác", () =>
        {
            var world = TestWorld.Create();
            world.Gateway.FailNext("CS-08-04", FaultKind.Timeout);
            world.Gateway.FailNext("CS-01-02", FaultKind.Timeout);

            world.Timer.TickNow();

            var port = world.Store.FindChargingPoint("CS-08-04");
            Assert.True(port is not null, "Phải tìm thấy CS-08-04");
            Assert.Equal(DeviceStatus.Offline, port!.Status, "Cổng mất kết nối phải OFFLINE");

            var newlyOffline = world.Store.FindChargingPoint("CS-01-02");
            Assert.Equal(DeviceStatus.Offline, newlyOffline!.Status, "CS-01-02 vừa mất kết nối phải OFFLINE");
            Assert.True(world.Logger.Contains("WARN", "CS-01-02"), "Phải ghi log mất kết nối");

            foreach (var hubId in new[] { "HUB-01", "HUB-02", "HUB-09" })
            {
                var hub = world.Store.GetHub(hubId);
                Assert.Equal(world.Clock.Now, hub.LastUpdatedAt, $"{hubId} vẫn phải được cập nhật");
                Assert.Equal(ConnectionStatus.Online, hub.Connection);
            }
        });

        runner.Add("TCSYS02-03", "Dữ liệu cũ quá 10 phút mới cảnh báo", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-08");
            hub.LastUpdatedAt = world.Clock.Now.AddMinutes(-14);

            Assert.True(world.Timer.CheckStaleData(hub), "14 phút phải là dữ liệu cũ");
            Assert.True(
                world.Notification.OperatorAlerts.Any(a => a.Type == AlertType.StaleData && a.HubId == "HUB-08"),
                "Phải phát cảnh báo STALE_DATA");

            var boundaryHub = world.Store.GetHub("HUB-01");
            boundaryHub.LastUpdatedAt = world.Clock.Now.AddMinutes(-10);
            Assert.False(world.Timer.CheckStaleData(boundaryHub), "Đúng 10 phút chưa tính là cũ");
        });
    }
}
