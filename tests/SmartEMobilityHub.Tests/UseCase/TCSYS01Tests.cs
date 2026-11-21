using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.UseCase;

/// <summary>UC-SYS-01 / UC-OP-04 — Bảng 62 (TCSYS01-01..04).</summary>
public static class TCSYS01Tests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("TCSYS01-01", "Xếp hạng ưu tiên sạc theo pin, thời gian cần và cao điểm", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-05");

            // Chỉ còn 2 cổng trống để chứng minh 2 yêu cầu đầu được cấp cổng.
            hub.ChargingPoints[2].Status = DeviceStatus.Faulty;
            hub.ChargingPoints[3].Status = DeviceStatus.Faulty;

            var a = TestWorld.AddRequest(world, "CR-A", "XE-115", hub.Id, 10, 30);
            var b = TestWorld.AddRequest(world, "CR-B", "XE-116", hub.Id, 60, 120);
            var c = TestWorld.AddRequest(world, "CR-C", "XE-112", hub.Id, 30, 60);

            var plan = world.Charging.RunSchedule(hub);

            Assert.Equal(3, plan.OrderedRequests.Count);
            Assert.Equal("CR-A", plan.OrderedRequests[0].RequestId, "Pin 10% phải đứng đầu");
            Assert.Equal("CR-C", plan.OrderedRequests[1].RequestId);
            Assert.Equal("CR-B", plan.OrderedRequests[2].RequestId);

            Assert.InRange(a.PriorityScore, 83.12, 83.13, "Điểm CR-A = 83,125");
            Assert.InRange(c.PriorityScore, 68.74, 68.76, "Điểm CR-C = 68,75");
            Assert.InRange(b.PriorityScore, 44.99, 45.01, "Điểm CR-B = 45");

            Assert.Equal(2, hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Charging), "Chỉ 2 cổng trống");
            Assert.True(a.AssignedPortId is not null, "CR-A phải được cấp cổng");
            Assert.True(c.AssignedPortId is not null, "CR-C phải được cấp cổng");
            Assert.True(b.AssignedPortId is null, "CR-B phải chờ vì hết cổng");
        });

        runner.Add("TCSYS01-02", "Đổi trọng số: tổng khác 1 bị từ chối, cấu hình cũ giữ nguyên", () =>
        {
            var world = TestWorld.Create();
            var request = TestWorld.AddRequest(world, "CR-X", "XE-115", "HUB-05", 10, 30);
            var before = world.Charging.CalculatePriorityScore(request);

            Assert.Throws<IllegalArgumentException>(
                () => world.Charging.UpdatePriorityRules(0.6, 0.6, 0.2),
                "Tổng 1,4 phải bị từ chối");

            Assert.Equal(0.5, world.Charging.WeightBattery, "Trọng số cũ phải giữ nguyên");
            Assert.Equal(before, world.Charging.CalculatePriorityScore(request), "Điểm không được đổi");

            world.Charging.UpdatePriorityRules(0.4, 0.4, 0.2);
            Assert.Equal(0.4, world.Charging.WeightBattery);
            Assert.InRange(world.Charging.CalculatePriorityScore(request), 80.99, 81.01, "Điểm mới = 81");
        });

        runner.Add("TCSYS01-03", "Kế hoạch vượt công suất bị phát hiện và không cấp quá tải", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-05");
            hub.PowerLimitKw = 20;

            TestWorld.AddRequest(world, "CR-1", "XE-115", hub.Id, 10, 30);
            TestWorld.AddRequest(world, "CR-2", "XE-116", hub.Id, 20, 60);
            TestWorld.AddRequest(world, "CR-3", "XE-112", hub.Id, 30, 90);
            TestWorld.AddRequest(world, "CR-4", "XE-114", hub.Id, 40, 120);

            var plan = world.Charging.RunSchedule(hub);

            Assert.Equal(28, plan.TotalPowerKw, "4 yêu cầu × 7 kW");
            Assert.False(world.Charging.CheckPowerLimit(hub, plan), "28 kW > 20 kW phải là vượt giới hạn");
            Assert.Equal(2, hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Charging), "Chỉ cấp 2 cổng (14 kW)");
            Assert.Equal(2, plan.OrderedRequests.Count(r => r.AssignedPortId is not null));
            Assert.True(
                world.Notification.OperatorAlerts.Any(a => a.Type == AlertType.HubOverload && a.Message.Contains("vượt giới hạn")),
                "Operator phải nhận đề xuất giãn tiến độ");
        });

        runner.Add("TCSYS01-04", "Cổng lỗi: chuyển xe sang cổng trống, hết cổng thì đưa lên đầu hàng chờ", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-07");
            var failedPort = hub.ChargingPoints[0];

            failedPort.Status = DeviceStatus.Charging;
            failedPort.AssignedVehicleCode = "XE-121";
            var vehicle = world.Store.FindVehicle("XE-121")!;
            vehicle.Status = DeviceStatus.Charging;
            vehicle.AssignedPortId = failedPort.Id;
            var request = TestWorld.AddRequest(world, "CR-F", "XE-121", hub.Id, 30, 60);
            request.AssignedPortId = failedPort.Id;

            Assert.True(world.Charging.HandleFaultyChargingPoint(failedPort), "Còn cổng trống thì phải chuyển được xe");
            Assert.Equal(DeviceStatus.Faulty, failedPort.Status);
            Assert.Equal("XE-121", hub.ChargingPoints[1].AssignedVehicleCode, "Xe phải sang cổng 02");
            Assert.Equal(DeviceStatus.Charging, vehicle.Status);
            Assert.True(
                world.Notification.OperatorAlerts.Any(a => a.Type == AlertType.ChargerFault),
                "Operator phải nhận cảnh báo cổng lỗi");

            // Trường hợp không còn cổng trống.
            var second = TestWorld.Create();
            var secondHub = second.Store.GetHub("HUB-07");
            var onlyPort = secondHub.ChargingPoints[0];
            onlyPort.Status = DeviceStatus.Charging;
            onlyPort.AssignedVehicleCode = "XE-121";
            foreach (var port in secondHub.ChargingPoints.Skip(1)) port.Status = DeviceStatus.Faulty;

            var waiting = TestWorld.AddRequest(second, "CR-Q", "XE-121", secondHub.Id, 40, 60);
            waiting.AssignedPortId = onlyPort.Id;

            Assert.False(second.Charging.HandleFaultyChargingPoint(onlyPort), "Không còn cổng thì trả false");
            Assert.Equal(DeviceStatus.NeedsCharge, second.Store.FindVehicle("XE-121")!.Status, "Xe cần sạc lại");
            Assert.Equal(0, waiting.QueuePosition, "Yêu cầu phải lên đầu hàng chờ");
            Assert.True(waiting.AssignedPortId is null, "Yêu cầu không được giữ cổng đã lỗi");
        });
    }
}
