using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Services;
using SmartEMobilityHub.Tests.Support;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Tests.StudentApp;

/// <summary>
/// Test cho logic app Sinh viên trên mạng lưới dùng chung với Trung tâm điều hành (DC-51, DC-54).
/// Mỗi test kiểm tra cả hai phía: những gì sinh viên thấy và những gì Operator thấy trong Core — APP-01..16, MAP-01..02.
/// </summary>
public static class StudentAppTests
{
    private const string Hub = "HUB-01";

    public static void Register(TestRunner runner)
    {
        runner.Add("APP-01", "Đặt xe: giữ 15 phút; Operator thấy ngay xe Đã đặt và đặt chỗ của sinh viên trong Core", () =>
        {
            var world = StudentWorld.Create();
            var result = world.App.Reservations.ReserveVehicle("EB-1024");

            Assert.True(result.Ok, "Đặt được EB-1024");
            Assert.Equal(world.Now.AddMinutes(DemoConfig.HoldMinutes), result.Value!.HoldUntil, "Giữ xe 15 phút");
            Assert.Equal(VehicleStatus.Reserved, world.App.Store.GetVehicle("EB-1024")!.Status, "Sinh viên thấy Đã đặt");
            Assert.Equal(CoreDomain.DeviceStatus.Reserved, world.Network.Store.FindVehicle("EB-1024")!.Status, "Operator thấy RESERVED");
            var core = world.Network.Store.Reservations.Single(r => r.Id == result.Value.Id);
            Assert.Equal(world.App.Store.StudentId, core.UserId, "Đặt chỗ mang mã sinh viên");
            Assert.True(ReferenceEquals(core, result.Value.Core), "Một đối tượng duy nhất, không có bản sao để đồng bộ");
        });

        runner.Add("APP-02", "Mỗi sinh viên giữ 1 xe; Operator tạm dừng Hub thì không đặt được và xe vẫn sẵn sàng", () =>
        {
            var world = StudentWorld.Create();
            world.App.Reservations.ReserveVehicle("EB-1024");
            Assert.Equal("AlreadyActive", world.App.Reservations.ReserveVehicle("EM-2051").Error, "Xe thứ hai bị từ chối");

            var other = StudentWorld.Create();
            other.Network.Incidents.SuspendHub(Hub, "OP-01");
            Assert.True(other.App.Store.GetHub(Hub).IsSuspended, "Sinh viên thấy Hub tạm dừng ngay, không cần đồng bộ");
            Assert.Equal("HubSuspended", other.App.Reservations.ReserveVehicle("EB-1024").Error, "Không đặt được ở Hub tạm dừng");
            Assert.Equal(VehicleStatus.Available, other.App.Store.GetVehicle("EB-1024")!.Status, "Xe không bị giữ nhầm");
        });

        runner.Add("APP-03", "Hết 15 phút giữ xe theo đúng quy tắc của Core: phút 14 chưa huỷ, quá 15 phút thì hết hạn và có thông báo", () =>
        {
            var world = StudentWorld.Create();
            world.App.Reservations.ReserveVehicle("EB-1024");
            world.App.Watcher.Poll();

            world.Advance(14);
            Assert.Equal(0, world.App.Reservations.AutoCancelExpired().Count, "Phút thứ 14 vẫn giữ");

            world.Advance(1.5);
            var expired = world.App.Reservations.AutoCancelExpired();
            Assert.Equal(1, expired.Count, "Quá 15 phút thì huỷ");
            Assert.Equal(ReservationState.Expired, expired[0].State, "Sinh viên thấy Hết hạn");
            Assert.Equal(CoreDomain.ReservationStatus.Cancelled, expired[0].Core.Status, "Core ghi CANCELLED như TCSYS04");
            Assert.Equal(CoreDomain.DeviceStatus.Available, world.Network.Store.FindVehicle("EB-1024")!.Status, "Xe trả về sẵn sàng");

            world.App.Watcher.Poll();
            Assert.True(world.App.Store.Notifications.Any(n => n.Title == "Đặt chỗ đã bị hủy" && n.Body.Contains("EB-1024")),
                "Sinh viên nhận thông báo hết hạn");
        });

        runner.Add("APP-04", "Quét nhầm xe: còn 2 lần thử, lần thứ 3 sai thì hết lượt nhưng đặt chỗ vẫn giữ", () =>
        {
            var world = StudentWorld.Create();
            world.App.Reservations.ReserveVehicle("EB-1024");
            var pickUp = world.App.PickUp;

            Assert.Equal("Wrong", pickUp.VerifyPickUpCode("EM-2051", null).Error, "Lần sai 1");
            Assert.Equal("Wrong", pickUp.VerifyPickUpCode("EM-2051", null).Error, "Lần sai 2");
            Assert.Equal("NoAttempts", pickUp.VerifyPickUpCode("EM-2051", null).Error, "Lần sai 3 hết lượt");
            Assert.Equal(ReservationState.Active, world.App.Reservations.ActiveVehicleReservation!.State, "Đặt chỗ vẫn còn");
        });

        runner.Add("APP-05", "Nhận và trả xe: chỗ đỗ của hai Hub trên Core cập nhật; tính 11 phút × 1.500đ, trừ 1 phút gói tháng", () =>
        {
            var world = StudentWorld.Create();
            var from = world.Network.Store.GetHub(Hub);
            var to = world.Network.Store.GetHub("HUB-04");
            var occupiedFrom = from.OccupiedParkingSlots;
            var occupiedTo = to.OccupiedParkingSlots;

            var trip = StartTrip(world, "EB-1024");
            Assert.Equal(occupiedFrom - 1, from.OccupiedParkingSlots, "Nhận xe trả một chỗ đỗ ở Hub đi");
            Assert.Equal(CoreDomain.DeviceStatus.InUse, world.Network.Store.FindVehicle("EB-1024")!.Status, "Operator thấy xe đang chạy");

            world.Advance(10.5);
            var done = world.App.Billing.Complete("HUB-04");

            Assert.Equal(11, done.Invoice!.Minutes, "Tính theo phút bắt đầu");
            Assert.Equal(11 * DemoConfig.RentalPricePerMinute, done.Invoice.RentalFee, "16.500đ tiền thuê");
            Assert.Equal(15000, done.Invoice.Total, "Trừ 1 phút gói tháng, còn 15.000đ");
            Assert.Equal(occupiedTo + 1, to.OccupiedParkingSlots, "Trả xe chiếm một chỗ ở Hub đến");
            var vehicle = world.App.Store.GetVehicle("EB-1024")!;
            Assert.Equal("HUB-04", vehicle.HubId, "Xe nằm ở Hub đến");
            Assert.Equal(VehicleStatus.Available, vehicle.Status, "Xe sẵn sàng cho người khác");
            Assert.Equal(CoreDomain.ReservationStatus.Completed,
                world.Network.Store.Reservations.Single(r => r.VehicleCode == "EB-1024" && r.UserId == world.App.Store.StudentId).Status,
                "Đặt chỗ trên Core hoàn tất");
            Assert.Equal(trip.Id, done.Id, "Đúng chuyến");
        });

        runner.Add("APP-06", "Trả xe ở Hub hết chỗ thật trên Core: gợi ý Hub gần nhất còn chỗ và miễn phí thêm 10 phút", () =>
        {
            var world = StudentWorld.Create();
            StartTrip(world, "EB-1024");
            world.Advance(20);
            var ktx = world.Network.Store.GetHub("HUB-02");
            ktx.OccupiedParkingSlots = ktx.ParkingCapacity;

            var check = world.App.Billing.CheckReturn("HUB-02");
            Assert.Equal("HubFull", check.Error, "Hub đầy bị từ chối");
            Assert.True(check.Value is not null && check.Value.Id != "HUB-02" && check.Value.FreeSlots > 0, "Có Hub thay thế còn chỗ");

            var done = world.App.Billing.Complete(check.Value!.Id);
            Assert.Equal(DemoConfig.ReturnExtraFreeMinutes, done.Invoice!.FreeExtraMinutes, "Miễn 10 phút");
            Assert.Equal((20 - 10 - 1) * DemoConfig.RentalPricePerMinute, done.Invoice.Total, "20 phút − 10 miễn phí − 1 phút gói");
        });

        runner.Add("APP-07", "Đỗ xe cá nhân: chiếm chỗ trên Core; huỷ dưới 5 phút mất 10.000đ, đúng 5 phút thì không", () =>
        {
            var world = StudentWorld.Create();
            var hub = world.Network.Store.GetHub("HUB-04");
            var occupied = hub.OccupiedParkingSlots;
            var reserved = world.App.Parking.ReserveParking("HUB-04", "PV1", world.Now.AddMinutes(4), 60);
            Assert.True(reserved.Ok, "Đặt được chỗ đỗ");
            Assert.Equal(occupied + 1, hub.OccupiedParkingSlots, "Operator thấy Hub thêm một chỗ đã dùng");

            world.App.Store.ParkingReservations.Add(Parking("PR-EDGE", world.Now.AddMinutes(5)));
            Assert.Equal(DemoConfig.LateCancellationFee, world.App.Reservations.PreviewCancel(BookingKind.Parking, reserved.Value!.Id).Value,
                "Dưới 5 phút là huỷ muộn");
            Assert.Equal(0, world.App.Reservations.PreviewCancel(BookingKind.Parking, "PR-EDGE").Value, "Đúng 5 phút chưa tính muộn");

            Assert.Equal(DemoConfig.LateCancellationFee, world.App.Reservations.Cancel(BookingKind.Parking, reserved.Value.Id), "Huỷ thu phí");
            Assert.Equal(occupied, hub.OccupiedParkingSlots, "Trả lại chỗ đỗ trên Core");
        });

        runner.Add("APP-08", "Đã nhận xe thì không huỷ được đặt xe; nút tua của Demo panel tua cả đồng hồ mạng lưới", () =>
        {
            var world = StudentWorld.Create();
            StartTrip(world, "EB-1024");
            var reservation = world.App.Store.VehicleReservations.Single(r => r.VehicleCode == "EB-1024");
            Assert.Equal("Started", world.App.Reservations.PreviewCancel(BookingKind.Vehicle, reservation.Id).Error,
                "Chuyến đã bắt đầu thì phải trả xe, không huỷ");

            var before = world.Network.Clock.Now;
            world.App.Clock.FastForward(15);
            Assert.Equal(before.AddMinutes(15), world.Network.Clock.Now, "Một đồng hồ cho cả hai app");
        });

        runner.Add("APP-09", "Yêu cầu sạc vào đúng hàng chờ của Core: còn cổng thì được cấp ngay, Operator thấy xe ở cổng", () =>
        {
            var world = StudentWorld.Create();
            StartTrip(world, "EB-1024");

            var result = world.App.Charging.SubmitChargingRequest("EB-1024", Hub);
            Assert.True(result.Ok, "Gửi được yêu cầu");
            Assert.Equal(ChargingRequestState.Assigned, result.Value!.State, "HUB-01 còn cổng trống nên được cấp ngay");
            var port = world.Network.Store.FindChargingPoint(result.Value.Core.AssignedPortId!)!;
            Assert.Equal("EB-1024", port.AssignedVehicleCode, "Cổng trên Core giữ cho xe của sinh viên");
            Assert.Equal(CoreDomain.DeviceStatus.InUse, world.Network.Store.FindVehicle("EB-1024")!.Status,
                "Xe còn đang chạy nên vẫn là Đang dùng");
            Assert.Equal("Duplicate", world.App.Charging.SubmitChargingRequest("EB-1024", Hub).Error, "Không tạo yêu cầu trùng");
        });

        runner.Add("APP-10", "Tất cả cổng bận: xếp hàng #1; Demo \"Cấp cổng sạc\" có xe sạc xong, Core cấp cổng và sinh viên được báo", () =>
        {
            var world = StudentWorld.Create();
            world.App.Toggles.AllChargingPointsBusy = true;
            StartTrip(world, "EB-1024");

            var result = world.App.Charging.SubmitChargingRequest("EB-1024", Hub);
            var request = result.Value!;
            Assert.Equal(ChargingRequestState.InQueue, request.State, "Không còn cổng trống nên vào hàng chờ");
            Assert.Equal(1, request.QueuePosition, "Đứng đầu hàng chờ của HUB-01");
            Assert.Equal(1, world.App.Store.GetHub(Hub).QueueLength, "Operator thấy 1 yêu cầu đang chờ");
            world.App.Watcher.Poll();

            var assigned = world.App.Charging.AssignNextChargingPoint();
            Assert.True(ReferenceEquals(request, assigned), "Yêu cầu của sinh viên được cấp cổng");
            Assert.Equal(ChargingRequestState.Assigned, request.State, "Đã có cổng");
            world.App.Watcher.Poll();
            Assert.True(world.App.Store.Notifications.Any(n => n.Title == "Đã có cổng sạc"), "Sinh viên được báo có cổng");

            world.App.Reservations.Cancel(BookingKind.ChargingRequest, request.Id);
            Assert.Equal(ChargingRequestState.Cancelled, request.State, "Huỷ yêu cầu");
            Assert.False(world.Network.Store.ChargingRequests.Any(r => r.RequestId == request.Id), "Core bỏ yêu cầu khỏi hàng chờ");
        });

        runner.Add("APP-11", "Báo lỗi: EB-1031 gộp vào sự cố đã có; xe mới tạo sự cố trong hàng đợi Operator, xe chuyển Đang kiểm tra", () =>
        {
            var world = StudentWorld.Create();
            var incidents = world.Network.Store.Incidents.Count;

            var linked = world.App.Incidents.ReceiveStudentReport(ReportObjectKind.Vehicle, "eb-1031", null, null, "Khóa không mở", "Vẫn không mở", null);
            Assert.Equal("Linked", linked.Message, "Báo cáo trùng được gộp");
            Assert.Equal(incidents, world.Network.Store.Incidents.Count, "Không tạo sự cố mới");

            var created = world.App.Incidents.ReceiveStudentReport(ReportObjectKind.Vehicle, "EM-2051", null, null, "Phanh kém", "Phanh trước yếu", 1.2);
            Assert.True(created.Ok && created.Message == "", "Tạo sự cố mới");
            var record = world.Network.Store.GetIncident(created.Value!.Code);
            Assert.Equal(CoreDomain.IncidentSource.StudentReport, record.Source, "Nguồn là sinh viên");
            Assert.True(record.ReporterIds.Contains(world.App.Store.StudentId), "Ghi người báo");
            Assert.Equal(CoreDomain.DeviceStatus.UnderCheck, world.Network.Store.FindVehicle("EM-2051")!.Status, "Xe chuyển Đang kiểm tra");
            Assert.Equal(VehicleStatus.UnderCheck, world.App.Store.GetVehicle("EM-2051")!.Status, "Sinh viên thấy cùng trạng thái");
            Assert.Equal("PhotoTooLarge",
                world.App.Incidents.ReceiveStudentReport(ReportObjectKind.Vehicle, "EB-1040", null, null, "Khác", "", 7.2).Error, "Ảnh quá 5 MB");
        });

        runner.Add("APP-12", "Báo lỗi cổng sạc dùng mã cổng của Core; Operator xử lý xong thì sinh viên thấy Đã xử lý và được báo", () =>
        {
            var world = StudentWorld.Create();
            var result = world.App.Incidents.ReceiveStudentReport(ReportObjectKind.ChargingPoint, "", Hub, 2, "Không sạc được", "", null);
            Assert.True(result.Ok, "Gửi được báo cáo cổng sạc");
            Assert.Equal("CS-01-02", result.Value!.ObjectCode, "Mã cổng của Core");
            Assert.Equal(PointStatus.OutOfService, world.App.Store.GetHub(Hub).Points.Single(p => p.Number == 2).Status, "Cổng ngưng nhận xe");
            world.App.Watcher.Poll();

            var manager = world.Network.Incidents;
            var id = result.Value.Code;
            manager.LockDevice(id);
            var task = manager.CreateMaintenanceTask(id);
            manager.AssignTechnician(task.TaskId, manager.SuggestTechnician(Hub)!.TechId);
            manager.ResolveIncident(id, "Đã thay đầu sạc");

            Assert.Equal(IncidentStatus.Resolved, result.Value.Status, "Sinh viên thấy Đã xử lý");
            world.App.Watcher.Poll();
            Assert.True(world.App.Store.Notifications.Any(n => n.Title == "Sự cố đã được xử lý" && n.Body.Contains(id)), "Có thông báo");
            Assert.Equal(PointStatus.Free, world.App.Store.GetHub(Hub).Points.Single(p => p.Number == 2).Status, "Cổng dùng lại được");
        });

        runner.Add("APP-13", "Operator tạm dừng Hub đang giữ xe cho sinh viên: đặt xe bị huỷ, xe sẵn sàng lại, sinh viên được báo", () =>
        {
            var world = StudentWorld.Create();
            var reservation = world.App.Reservations.ReserveVehicle("EB-1024").Value!;
            world.App.Watcher.Poll();

            world.Network.Incidents.SuspendHub(Hub, "OP-01");

            Assert.Equal(ReservationState.Cancelled, reservation.State, "Đặt xe bị huỷ");
            Assert.Equal(VehicleStatus.Available, world.App.Store.GetVehicle("EB-1024")!.Status, "Xe được trả lại");
            world.App.Watcher.Poll();
            var titles = world.App.Store.Notifications.Select(n => n.Title).ToList();
            Assert.True(titles.Contains("Hub tạm dừng hoạt động"), "Báo Hub tạm dừng");
            Assert.True(titles.Contains("Đặt xe đã bị hủy"), "Báo đặt xe bị huỷ");

            world.Network.Incidents.ResumeHub(Hub, "OP-01");
            world.App.Watcher.Poll();
            Assert.True(world.App.Store.Notifications.Any(n => n.Title == "Hub hoạt động trở lại"), "Báo Hub mở lại");
        });

        runner.Add("APP-14", "Không xác nhận mở khoá: Core huỷ đặt xe, giữ xe RESERVED, sự cố hiện trong danh sách của sinh viên", () =>
        {
            var world = StudentWorld.Create();
            var reservation = world.App.Reservations.ReserveVehicle("EM-2072").Value!;
            Assert.True(world.App.PickUp.VerifyPickUpCode("EM-2072", null).Ok, "Quét đúng xe");

            var incident = world.App.PickUp.ReceiveUnlockTimeout(reservation);

            Assert.Equal(CoreDomain.IncidentSource.UnlockTimeout, incident.Source, "Sự cố lỗi mở khoá");
            Assert.Equal(ReservationState.Cancelled, reservation.State, "Đặt xe bị huỷ (TCOP03-05)");
            Assert.Equal(VehicleStatus.Reserved, world.App.Store.GetVehicle("EM-2072")!.Status, "Xe được giữ chờ kiểm tra");
            Assert.True(world.App.Store.Incidents.Any(i => i.Code == incident.Code), "Sinh viên thấy sự cố của mình");
            world.App.Watcher.Poll();
            Assert.False(world.App.Store.Notifications.Any(n => n.Title == "Đặt xe đã bị hủy"),
                "Không báo trùng: màn hình lỗi mở khoá đã báo");
        });

        runner.Add("APP-15", "Operator khoá xe hoặc điều phối xe thì app Sinh viên thấy ngay (một nguồn dữ liệu)", () =>
        {
            var world = StudentWorld.Create();
            var report = world.App.Incidents.ReceiveStudentReport(ReportObjectKind.Vehicle, "EB-1040", null, null, "Khác", "Kêu lạ", null).Value!;
            world.Network.Incidents.LockDevice(report.Code);
            Assert.Equal(VehicleStatus.Faulty, world.App.Store.GetVehicle("EB-1040")!.Status, "Xe bị khoá → không đặt được");
            Assert.False(world.App.Store.GetVehicle("EB-1040")!.IsAvailable, "Không còn sẵn sàng");

            var core = world.Network.Store.FindVehicle("EB-2001")!;
            core.HubId = "HUB-11";
            Assert.True(world.App.Vehicles.ListVehicles("HUB-11").Any(v => v.Code == "EB-2001"), "Xe hiện ở Hub mới");
            Assert.False(world.App.Vehicles.ListVehicles("HUB-04").Any(v => v.Code == "EB-2001"), "Không còn ở Hub cũ");
        });

        runner.Add("APP-16", "App Sinh viên thấy đủ mạng lưới của Operator: 12 Hub, có xe của cả hai kịch bản, Hub Nhà văn hóa SV hết xe", () =>
        {
            var world = StudentWorld.Create();
            Assert.Equal(world.Network.Store.Hubs.Count, world.App.Store.Hubs.Count, "Cùng số Hub");
            Assert.Equal(12, world.App.Store.Hubs.Count, "12 Hub của mạng lưới");
            Assert.True(world.App.Store.GetVehicle("XE-101") is not null && world.App.Store.GetVehicle("EB-1024") is not null,
                "Xe của Operator và xe của kịch bản sinh viên nằm chung");
            Assert.Equal(0, world.App.Vehicles.AvailableCount(world.App.Store.GetHub("HUB-11")), "UC-01 7a: Hub không còn xe sẵn sàng");
            Assert.True(ReferenceEquals(world.App.Store.GetHub(Hub), world.App.Store.Hubs.Single(h => h.Id == Hub)),
                "Mỗi Hub chỉ có một view (so sánh tham chiếu giữa các lần vẽ màn hình)");
            Assert.True(world.App.Store.Incidents.Any(i => i.ObjectCode == "EB-1031"), "Báo lỗi cũ của sinh viên có trong lịch sử");
        });

        runner.Add("APP-17", "Các luồng còn lại của app chạy được trên dữ liệu Core: hành trình, lịch sạc xe cá nhân, cổng hỏng, lịch sử", () =>
        {
            var world = StudentWorld.Create();
            var app = world.App;

            var journey = app.Journey.PlanJourney("Thư viện");
            Assert.True(journey.RouteAvailable && journey.StartHub is not null && journey.EndHub!.Id == "HUB-04",
                "Đi Thư viện Trung tâm: Hub đến là HUB-04");
            Assert.True(app.Vehicles.AvailableCount(journey.StartHub!) > 0, "Hub xuất phát có xe");
            Assert.Equal(3, app.Vehicles.NearestHubsWithAvailable(app.Store.GetHub("HUB-11"), 3).Count, "Gợi ý 3 Hub gần nhất còn xe");
            Assert.True(app.Parking.FindAvailableHubsWithin1Km(Hub).Count > 0, "Có Hub còn chỗ trong 1 km");

            Assert.Equal("DurationTooLong",
                app.Booking.ScheduleCharging("HUB-04", "PV1", world.Now.AddHours(1), 150, 80).Error, "Thư viện chỉ cho 120 phút");
            var session = app.Booking.ScheduleCharging(Hub, "PV1", world.Now.AddHours(1), 90, 80);
            Assert.True(session.Ok, "Đặt lịch sạc ở HUB-01");
            var oldPoint = session.Value!.PointNumber;
            Assert.Equal("Moved", app.Booking.ChargingPointOutOfService(true), "Cổng hỏng: chuyển sang cổng khác");
            Assert.True(session.Value.PointNumber != oldPoint, "Đã đổi cổng");
            Assert.Equal(CoreDomain.DeviceStatus.Faulty,
                world.Network.Store.GetHub(Hub).ChargingPoints.Single(p => p.Number == oldPoint).Status, "Operator thấy cổng FAULTY");

            foreach (var hub in app.Store.Hubs) app.Charging.GetChargingEstimate("EB-1024", hub.Id);
            Assert.True(app.History.Entries().Count >= 2, "Lịch sử 3 tháng có chuyến đi mẫu");
            Assert.True(app.Reservations.ListActive().Any(i => i.Kind == BookingKind.ChargingSession), "Danh sách đặt chỗ có lịch sạc");
            app.Reservations.ListFinished();
        });

        runner.Add("MAP-01", "Trạng thái xe Core → app: khoá / đang sửa / hỏng / mất kết nối đều là Hỏng, còn lại giữ nghĩa", () =>
        {
            Assert.Equal(VehicleStatus.Available, NetworkMapping.StudentStatus(CoreDomain.DeviceStatus.Available), "Sẵn sàng");
            Assert.Equal(VehicleStatus.NeedsCharge, NetworkMapping.StudentStatus(CoreDomain.DeviceStatus.NeedsCharge), "Cần sạc");
            Assert.Equal(VehicleStatus.Relocating, NetworkMapping.StudentStatus(CoreDomain.DeviceStatus.Relocating), "Đang điều chuyển");
            foreach (var status in new[] { CoreDomain.DeviceStatus.Locked, CoreDomain.DeviceStatus.UnderRepair,
                         CoreDomain.DeviceStatus.Faulty, CoreDomain.DeviceStatus.Offline })
                Assert.Equal(VehicleStatus.Faulty, NetworkMapping.StudentStatus(status), $"{status} → Hỏng");
        });

        runner.Add("MAP-02", "Đọc số cổng từ mã cổng của Core: CS-11-04 → 4; sai định dạng thì từ chối", () =>
        {
            Assert.True(NetworkMapping.TryParsePointNumber("CS-11-04", out var number) && number == 4, "Đọc số cổng");
            Assert.False(NetworkMapping.TryParsePointNumber("CS-11-xx", out _), "Mã sai định dạng");
        });
    }

    private static Trip StartTrip(StudentWorld world, string vehicleCode)
    {
        var reservation = world.App.Reservations.ReserveVehicle(vehicleCode).Value!;
        var verified = world.App.PickUp.VerifyPickUpCode(vehicleCode, null);
        Assert.True(verified.Ok, "Quét đúng xe");
        return world.App.PickUp.ConfirmStart(reservation)!;
    }

    private static ParkingReservation Parking(string id, DateTime start) => new()
    {
        Id = id,
        PrivateVehicleId = "PV1",
        HubId = "HUB-04",
        SlotNumber = "P-07",
        Start = start,
        DurationMinutes = 60,
        EntryCode = "PK-" + id,
        State = ReservationState.Active
    };
}
