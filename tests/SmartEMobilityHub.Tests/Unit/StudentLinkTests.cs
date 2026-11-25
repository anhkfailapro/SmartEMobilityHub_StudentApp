using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.Unit;

/// <summary>Liên thông app Sinh viên ↔ Trung tâm điều hành qua StudentAppGateway — LINK-01..09.</summary>
public static class StudentLinkTests
{
    private const string Student = "2310001";

    private static (TestWorld World, StudentAppGateway Gateway) Create()
    {
        var world = TestWorld.Create();
        var gateway = new StudentAppGateway(world.Store, world.Incidents, world.Charging, world.Reservations, world.Clock);
        gateway.EnsureVehicle("EB-1024", VehicleType.EBike, "HUB-01", "A-07", 82, DeviceStatus.Available);
        return (world, gateway);
    }

    public static void Register(TestRunner runner)
    {
        runner.Add("LINK-01", "Xe của app Sinh viên được đưa vào mạng lưới một lần, không đổi bộ đếm chỗ đỗ", () =>
        {
            var (world, gateway) = Create();
            var occupied = world.Store.GetHub("HUB-01").OccupiedParkingSlots;
            gateway.EnsureVehicle("EB-1024", VehicleType.EBike, "HUB-01", "A-07", 50, DeviceStatus.Faulty);

            Assert.Equal(1, world.Store.Vehicles.Count(v => v.Code == "EB-1024"), "Không thêm trùng");
            Assert.Equal(82, world.Store.FindVehicle("EB-1024")!.BatteryLevel, "Giữ dữ liệu lần đầu");
            Assert.Equal(occupied, world.Store.GetHub("HUB-01").OccupiedParkingSlots, "Bộ đếm chỗ đỗ giữ nguyên");
        });

        runner.Add("LINK-02", "Sinh viên đặt xe thì Operator thấy xe RESERVED và một đặt chỗ chờ nhận", () =>
        {
            var (world, gateway) = Create();
            gateway.ReserveVehicle("VR-101", Student, "EB-1024", "HUB-01", world.Clock.Now.AddMinutes(15));

            Assert.Equal(DeviceStatus.Reserved, world.Store.FindVehicle("EB-1024")!.Status, "Xe bị giữ");
            Assert.Equal(ReservationStatus.PendingPickup, gateway.FindReservation("VR-101")!.Status, "Đặt chỗ chờ nhận");
            Assert.Throws<IllegalStateException>(
                () => gateway.ReserveVehicle("VR-102", "khac", "EB-1024", "HUB-01", world.Clock.Now.AddMinutes(15)),
                "Một xe không thuộc hai đặt chỗ cùng lúc (NFR 2.2.2)");
        });

        runner.Add("LINK-03", "Operator tạm dừng Hub thì đặt xe đang giữ ở Hub đó bị huỷ và Hub không nhận đặt mới", () =>
        {
            var (world, gateway) = Create();
            gateway.ReserveVehicle("VR-101", Student, "EB-1024", "HUB-01", world.Clock.Now.AddMinutes(15));

            world.Incidents.SuspendHub("HUB-01", "OP-01");

            Assert.Equal(ReservationStatus.Cancelled, gateway.FindReservation("VR-101")!.Status, "Đặt xe bị huỷ");
            Assert.Equal(DeviceStatus.Available, world.Store.FindVehicle("EB-1024")!.Status, "Xe được trả lại");
            Assert.True(gateway.GetHubBlockReason("HUB-01") is not null, "Có lý do chặn");
            Assert.Throws<IllegalStateException>(
                () => gateway.ReserveVehicle("VR-103", Student, "EB-1024", "HUB-01", world.Clock.Now.AddMinutes(15)),
                "Hub tạm dừng không nhận đặt mới");
        });

        runner.Add("LINK-04", "Nhận xe rồi trả ở Hub khác: hai bộ đếm chỗ đỗ cập nhật, đặt chỗ hoàn tất", () =>
        {
            var (world, gateway) = Create();
            var metro = world.Store.GetHub("HUB-01");
            var library = world.Store.GetHub("HUB-04");
            var metroBefore = metro.OccupiedParkingSlots;
            var libraryBefore = library.OccupiedParkingSlots;

            gateway.ReserveVehicle("VR-101", Student, "EB-1024", "HUB-01", world.Clock.Now.AddMinutes(15));
            gateway.PickUp("VR-101");
            Assert.Equal(DeviceStatus.InUse, world.Store.FindVehicle("EB-1024")!.Status, "Đang dùng");
            Assert.Equal(metroBefore - 1, metro.OccupiedParkingSlots, "Hub nhận xe trống thêm một chỗ");

            gateway.ReturnVehicle("EB-1024", "HUB-04", "E-09", 15, needsCharge: true);
            var vehicle = world.Store.FindVehicle("EB-1024")!;
            Assert.Equal("HUB-04", vehicle.HubId, "Xe nằm ở Hub trả");
            Assert.Equal(DeviceStatus.NeedsCharge, vehicle.Status, "Pin thấp thì cần sạc");
            Assert.Equal(libraryBefore + 1, library.OccupiedParkingSlots, "Hub trả chiếm thêm một chỗ");
            Assert.Equal(ReservationStatus.Completed, gateway.FindReservation("VR-101")!.Status, "Đặt chỗ hoàn tất");
        });

        runner.Add("LINK-05", "Sinh viên huỷ đặt xe: xe sẵn sàng lại; huỷ lần hai hoặc sau khi nhận xe thì bỏ qua", () =>
        {
            var (world, gateway) = Create();
            gateway.ReserveVehicle("VR-101", Student, "EB-1024", "HUB-01", world.Clock.Now.AddMinutes(15));

            Assert.True(gateway.CloseReservation("VR-101", StudentAppGateway.CancelledByStudent), "Huỷ được");
            Assert.Equal(DeviceStatus.Available, world.Store.FindVehicle("EB-1024")!.Status, "Xe sẵn sàng");
            Assert.False(gateway.CloseReservation("VR-101", StudentAppGateway.CancelledByStudent), "Huỷ lần hai bỏ qua");
        });

        runner.Add("LINK-06", "Báo lỗi từ app Sinh viên vào hàng đợi sự cố của Operator, báo trùng thì gộp", () =>
        {
            var (world, gateway) = Create();
            var first = gateway.ReportProblem(Student, IncidentObjectType.Vehicle, "EB-1024", "Phanh yếu", "Phanh trước kém");
            var second = gateway.ReportProblem("2310002", IncidentObjectType.Vehicle, "EB-1024", "Phanh yếu", "Khó phanh");

            Assert.Equal(first.IncidentId, second.IncidentId, "Báo trùng gộp vào một sự cố");
            Assert.Equal("HUB-01", first.HubId, "Gắn đúng Hub của xe");
            Assert.True(world.Incidents.GetIncidentQueue().Any(i => i.IncidentId == first.IncidentId), "Có trong hàng đợi Operator");
            Assert.Equal(DeviceStatus.UnderCheck, world.Store.FindVehicle("EB-1024")!.Status, "Xe chuyển sang kiểm tra");
        });

        runner.Add("LINK-07", "Yêu cầu sạc vào hàng chờ Core và được lập lịch ngay nếu còn cổng trong công suất", () =>
        {
            var (world, gateway) = Create();
            var request = gateway.RequestCharging("CR-101", "EB-1024", "HUB-01", 18, world.Clock.Now.AddMinutes(30));

            Assert.True(world.Charging.GetQueue("HUB-01").Any(r => r.RequestId == "CR-101"), "Có trong hàng chờ HUB-01");
            Assert.True(request.PriorityScore > 0, "Đã tính điểm ưu tiên");
            Assert.Equal(request, gateway.RequestCharging("CR-101", "EB-1024", "HUB-01", 18, world.Clock.Now), "Gửi lại không tạo trùng");
        });

        runner.Add("LINK-08", "Huỷ yêu cầu sạc đã được cấp cổng thì trả cổng về sẵn sàng", () =>
        {
            var (world, gateway) = Create();
            var request = gateway.RequestCharging("CR-101", "EB-1024", "HUB-01", 18, world.Clock.Now.AddMinutes(30));
            var port = request.AssignedPortId;
            Assert.True(port is not null, "HUB-01 còn cổng và công suất nên được cấp ngay");

            Assert.True(gateway.CancelChargingRequest("CR-101"), "Huỷ được");
            Assert.Equal(DeviceStatus.Available, world.Store.FindChargingPoint(port!)!.Status, "Cổng sẵn sàng lại");
            Assert.True(gateway.FindChargingRequest("CR-101") is null, "Không còn trong hàng chờ");
        });

        runner.Add("LINK-10", "Yêu cầu sạc khi đang đi: xe vẫn đang dùng; trả đúng Hub có cổng thì vào sạc, trả Hub khác thì nhả cổng", () =>
        {
            var (world, gateway) = Create();
            gateway.ReserveVehicle("VR-101", Student, "EB-1024", "HUB-01", world.Clock.Now.AddMinutes(15));
            gateway.PickUp("VR-101");
            var request = gateway.RequestCharging("CR-101", "EB-1024", "HUB-01", 20, world.Clock.Now.AddMinutes(30));
            var port = request.AssignedPortId!;
            Assert.Equal(DeviceStatus.InUse, world.Store.FindVehicle("EB-1024")!.Status, "Đang đi thì vẫn là đang dùng");

            gateway.ReturnVehicle("EB-1024", "HUB-01", "A-20", 20, needsCharge: true);
            Assert.Equal(DeviceStatus.Charging, world.Store.FindVehicle("EB-1024")!.Status, "Trả đúng Hub thì vào sạc");

            var (world2, gateway2) = Create();
            gateway2.ReserveVehicle("VR-201", Student, "EB-1024", "HUB-01", world2.Clock.Now.AddMinutes(15));
            gateway2.PickUp("VR-201");
            var request2 = gateway2.RequestCharging("CR-201", "EB-1024", "HUB-01", 20, world2.Clock.Now.AddMinutes(30));
            var port2 = request2.AssignedPortId!;
            gateway2.ReturnVehicle("EB-1024", "HUB-04", "E-09", 20, needsCharge: true);
            Assert.Equal(DeviceStatus.Available, world2.Store.FindChargingPoint(port2)!.Status, "Trả Hub khác thì cổng được nhả");
            Assert.Equal(DeviceStatus.NeedsCharge, world2.Store.FindVehicle("EB-1024")!.Status, "Xe chờ sạc ở Hub mới");
            Assert.True(port.StartsWith("CS-01-"), "Cổng thuộc HUB-01");
        });

        runner.Add("LINK-09", "Đặt / trả chỗ đỗ xe cá nhân thay đổi mức sử dụng Hub trong giới hạn sức chứa", () =>
        {
            var (world, gateway) = Create();
            var hub = world.Store.GetHub("HUB-11");
            var before = hub.OccupiedParkingSlots;

            gateway.ChangeParkingOccupancy("HUB-11", 1);
            Assert.Equal(before + 1, hub.OccupiedParkingSlots, "Tăng một chỗ");
            gateway.ChangeParkingOccupancy("HUB-11", -1);
            Assert.Equal(before, hub.OccupiedParkingSlots, "Trả lại");
            gateway.ChangeParkingOccupancy("HUB-11", 1000);
            Assert.Equal(hub.ParkingCapacity, hub.OccupiedParkingSlots, "Không vượt sức chứa");
        });
    }
}
