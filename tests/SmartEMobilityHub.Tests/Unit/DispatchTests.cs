using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.Unit;

/// <summary>Điều phối xe giữa các Hub (nhánh operator-dispatching) — DSP-01..08.</summary>
public static class DispatchTests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("DSP-01", "Gợi ý chỉ chứa xe thật, đang sẵn sàng ở Hub cho, không trùng giữa các lệnh", () =>
        {
            var world = TestWorld.Create();
            var dispatcher = new VehicleRedistributor(world.Store, world.Clock);
            world.Store.GetHub("HUB-03").OccupiedParkingSlots = 6;
            world.Store.GetHub("HUB-07").OccupiedParkingSlots = 6;

            var orders = dispatcher.GenerateDispatchSuggestions();
            var codes = orders.SelectMany(o => o.VehicleIds).ToList();

            Assert.True(orders.Count > 0, "Phải có gợi ý khi có Hub dư và Hub thiếu");
            Assert.Equal(codes.Count, codes.Distinct().Count(), "Một xe không được nằm trong hai gợi ý");
            foreach (var order in orders)
            {
                foreach (var code in order.VehicleIds)
                {
                    var vehicle = world.Store.FindVehicle(code);
                    Assert.True(vehicle is not null, $"{code} phải là xe thật");
                    Assert.Equal(order.SourceHubId, vehicle!.HubId, $"{code} phải ở Hub cho");
                    Assert.Equal(DeviceStatus.Available, vehicle.Status, $"{code} phải đang sẵn sàng");
                }
            }
        });

        runner.Add("DSP-02", "Nhiều Hub cho không đổ vào một Hub nhận vượt mức thiếu", () =>
        {
            var world = TestWorld.Create();
            var dispatcher = new VehicleRedistributor(world.Store, world.Clock) { TargetLoadRatio = 0.35 };
            var receiver = world.Store.GetHub("HUB-03");
            receiver.OccupiedParkingSlots = 9; // 30% · mục tiêu ceil(30 × 0,35) = 11 → thiếu 2

            var orders = dispatcher.GenerateDispatchSuggestions();
            var incoming = orders.Where(o => o.TargetHubId == "HUB-03").Sum(o => o.VehicleIds.Count);

            Assert.Equal(2, incoming, "Tổng xe đổ vào HUB-03 phải đúng phần thiếu");
        });

        runner.Add("DSP-03", "Tạo lệnh từ chối xe không hợp lệ", () =>
        {
            var world = TestWorld.Create();
            var dispatcher = new VehicleRedistributor(world.Store, world.Clock);

            Assert.Throws<IllegalArgumentException>(
                () => dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "HUB-08-XE-01" }, "NV-01"), "Mã xe không tồn tại");
            Assert.Throws<IllegalArgumentException>(
                () => dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "XE-123" }, "NV-01"), "Xe đang sạc");
            Assert.Throws<IllegalArgumentException>(
                () => dispatcher.CreateOrder("HUB-08", "HUB-07", new[] { "XE-109" }, "NV-01"), "Xe ở Hub khác");

            dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "XE-129" }, "NV-01");
            Assert.Throws<IllegalArgumentException>(
                () => dispatcher.CreateOrder("HUB-08", "HUB-07", new[] { "XE-129" }, "NV-01"), "Xe đã có lệnh khác");
        });

        runner.Add("DSP-04", "Tạo lệnh từ chối khi Hub đích không đủ chỗ (tính cả xe đang trên đường tới)", () =>
        {
            var world = TestWorld.Create();
            var dispatcher = new VehicleRedistributor(world.Store, world.Clock);
            world.Store.GetHub("HUB-03").OccupiedParkingSlots = 28; // còn 2 chỗ

            dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "XE-129" }, "NV-01");
            dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "XE-139" }, "NV-01");
            Assert.Throws<IllegalArgumentException>(
                () => dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "XE-140" }, "NV-01"),
                "Hai chỗ đã dành cho hai lệnh trước");
        });

        runner.Add("DSP-05", "Không xuất phát khi xe đã đổi trạng thái sau lúc tạo lệnh", () =>
        {
            var world = TestWorld.Create();
            var dispatcher = new VehicleRedistributor(world.Store, world.Clock);
            var source = world.Store.GetHub("HUB-08");
            var order = dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "XE-129", "XE-139" }, "NV-01");
            var before = source.OccupiedParkingSlots;

            world.Store.FindVehicle("XE-139")!.Status = DeviceStatus.Reserved; // sinh viên vừa đặt

            Assert.False(dispatcher.ExecuteRedistribution(order.OrderId), "Có xe không còn sẵn sàng thì không xuất phát");
            Assert.Equal(DispatchOrderStatus.Pending, order.Status);
            Assert.Equal(before, source.OccupiedParkingSlots, "Không được trừ chỗ của Hub nguồn");
            Assert.Equal(DeviceStatus.Available, world.Store.FindVehicle("XE-129")!.Status, "Xe còn lại không bị đổi trạng thái");
        });

        runner.Add("DSP-06", "Hoàn tất cập nhật đúng hai Hub; Hub đích đầy thì chờ, không làm mất xe", () =>
        {
            var world = TestWorld.Create();
            var dispatcher = new VehicleRedistributor(world.Store, world.Clock);
            var source = world.Store.GetHub("HUB-08");
            var target = world.Store.GetHub("HUB-03");
            var order = dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "XE-129", "XE-139" }, "NV-01");
            var sourceBefore = source.OccupiedParkingSlots;
            var targetBefore = target.OccupiedParkingSlots;

            Assert.True(dispatcher.ExecuteRedistribution(order.OrderId), "Xuất phát được");
            Assert.Equal(sourceBefore - 2, source.OccupiedParkingSlots);
            Assert.Equal(DeviceStatus.Relocating, world.Store.FindVehicle("XE-129")!.Status);

            target.OccupiedParkingSlots = target.ParkingCapacity - 1; // Hub đích bất ngờ còn 1 chỗ
            Assert.False(dispatcher.CompleteOrder(order.OrderId), "Không đủ chỗ thì chưa hoàn tất");
            Assert.Equal(DispatchOrderStatus.InProgress, order.Status);

            target.OccupiedParkingSlots = targetBefore;
            Assert.True(dispatcher.CompleteOrder(order.OrderId), "Có chỗ thì hoàn tất");
            Assert.Equal(targetBefore + 2, target.OccupiedParkingSlots, "Hub đích nhận đủ 2 xe");
            Assert.Equal("HUB-03", world.Store.FindVehicle("XE-139")!.HubId);
            Assert.Equal(DeviceStatus.Available, world.Store.FindVehicle("XE-139")!.Status);
        });

        runner.Add("DSP-07", "Huỷ lệnh chưa xuất phát trả xe về cho lệnh khác; snapshot chứa lệnh điều phối", () =>
        {
            var world = TestWorld.Create();
            var dispatcher = new VehicleRedistributor(world.Store, world.Clock);
            var order = dispatcher.CreateOrder("HUB-08", "HUB-03", new[] { "XE-129" }, "NV-01");

            var snapshot = world.Store.Snapshot();
            Assert.Equal(1, snapshot.DispatchOrders.Count, "Snapshot phải có lệnh điều phối");
            Assert.False(ReferenceEquals(order, snapshot.DispatchOrders[0]), "Snapshot phải là bản sao");

            Assert.True(dispatcher.CancelOrder(order.OrderId), "Lệnh chờ huỷ được");
            Assert.Equal(DispatchOrderStatus.Cancelled, order.Status);
            dispatcher.CreateOrder("HUB-08", "HUB-07", new[] { "XE-129" }, "NV-01"); // không ném lỗi
        });

        runner.Add("DSP-08", "Ước tính lộ trình dùng GeoPoint và hằng số chung", () =>
        {
            var world = TestWorld.Create();
            var dispatcher = new VehicleRedistributor(world.Store, world.Clock);
            var a = world.Store.GetHub("HUB-08");
            var b = world.Store.GetHub("HUB-01");

            var route = dispatcher.CalculateEstimatedRoute(a, b);

            Assert.InRange(route["distanceKm"], a.Location.DistanceKmTo(b.Location) - 0.01, a.Location.DistanceKmTo(b.Location) + 0.01,
                "Khoảng cách khớp GeoPoint.DistanceKmTo");
            Assert.True(route["durationMinutes"] >= ServiceConstants.DispatchMinDurationMinutes, "Thời gian tối thiểu 3 phút");
        });
    }
}
