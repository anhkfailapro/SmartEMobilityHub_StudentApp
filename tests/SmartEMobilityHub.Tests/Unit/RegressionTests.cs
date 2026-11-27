using SmartEMobilityHub.Core.Adapters;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Core.Store;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.Unit;

/// <summary>
/// Test hồi quy cho các lỗi tìm thấy khi review bằng knowledge graph (GĐ8).
/// Mỗi test tái hiện một lỗi thật: trước khi sửa thì đỏ, sau khi sửa thì xanh.
/// </summary>
public static class RegressionTests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("REG-01", "Tình trạng quá tải kéo dài chỉ báo một lần, hết quá tải thì báo lại được", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-08"); // 54/60 = 90%

            world.Timer.TickNow();
            world.Clock.Advance(TimeSpan.FromSeconds(30));
            world.Timer.TickNow();
            world.Clock.Advance(TimeSpan.FromSeconds(30));
            world.Timer.TickNow();

            Assert.Equal(1, CountAlerts(world, AlertType.HubOverload, "HUB-08", "sức chứa"),
                "3 chu kỳ liên tiếp cùng một tình trạng quá tải chỉ được báo 1 lần");

            hub.OccupiedParkingSlots = 40;
            world.Timer.TickNow();
            hub.OccupiedParkingSlots = 55;
            world.Timer.TickNow();

            Assert.Equal(2, CountAlerts(world, AlertType.HubOverload, "HUB-08", "sức chứa"),
                "Hết quá tải rồi quá tải lại phải báo lần mới");
        });

        runner.Add("REG-02", "Mất kết nối kéo dài: mỗi Hub chỉ một cảnh báo DEVICE_OFFLINE", () =>
        {
            var world = TestWorld.Create();
            world.Gateway.SetNetworkDown(true);

            for (var i = 0; i < 3; i++)
            {
                world.Timer.TickNow();
                world.Clock.Advance(TimeSpan.FromSeconds(30));
            }

            Assert.Equal(1, CountAlerts(world, AlertType.DeviceOffline, "HUB-03"),
                "HUB-03 mất kết nối suốt 3 chu kỳ chỉ được báo 1 lần");
            Assert.Equal(world.Store.Hubs.Count, world.Notification.OperatorAlerts.Count(a => a.Type == AlertType.DeviceOffline),
                "Tổng cảnh báo DEVICE_OFFLINE phải bằng số Hub");
        });

        runner.Add("REG-03", "Dữ liệu cũ kéo dài chỉ báo STALE_DATA một lần", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-08");
            hub.LastUpdatedAt = world.Clock.Now.AddMinutes(-14);

            Assert.True(world.Timer.CheckStaleData(hub), "Lần 1: dữ liệu cũ");
            Assert.True(world.Timer.CheckStaleData(hub), "Lần 2: vẫn là dữ liệu cũ");

            Assert.Equal(1, CountAlerts(world, AlertType.StaleData, "HUB-08"),
                "Cùng một đợt dữ liệu cũ chỉ phát 1 cảnh báo");
        });

        runner.Add("REG-04", "Mạng cảm biến có lại thì cổng sạc trở về trạng thái thật, không kẹt OFFLINE", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-03");
            var before = hub.ChargingPoints.Select(p => p.Status).ToList();

            world.Gateway.SetNetworkDown(true);
            world.Timer.TickNow();
            Assert.True(hub.ChargingPoints.All(p => p.Status == DeviceStatus.Offline), "Đang mất mạng: cổng hiển thị OFFLINE");

            world.Gateway.SetNetworkDown(false);
            world.Clock.Advance(TimeSpan.FromSeconds(30));
            world.Timer.TickNow();

            Assert.Equal(ConnectionStatus.Online, hub.Connection, "Hub phải online lại");
            Assert.True(hub.ChargingPoints.Any(p => p.Status != DeviceStatus.Offline),
                "Có mạng lại thì cổng không được kẹt OFFLINE");
            Assert.Equal(before.Count(s => s == DeviceStatus.Offline),
                hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Offline),
                "Số cổng OFFLINE phải như trước khi mất mạng");
        });

        runner.Add("REG-05", "Kỹ thuật viên đã được gán thì không nhận thêm phiếu khác", () =>
        {
            var world = TestWorld.Create();
            var first = world.Incidents.ReceiveSensorAlert("HUB-08", "CS-08-07", AlertType.ChargerFault);
            var second = world.Incidents.ReceiveSensorAlert("HUB-09", "CS-09-02", AlertType.ChargerFault);

            var firstTask = world.Incidents.CreateMaintenanceTask(first.IncidentId);
            var secondTask = world.Incidents.CreateMaintenanceTask(second.IncidentId);
            var tech = world.Incidents.SuggestTechnician("HUB-08")!;

            Assert.True(world.Incidents.AssignTechnician(firstTask.TaskId, tech.TechId), "Gán lần đầu thành công");
            Assert.False(tech.IsAvailable(), "Kỹ thuật viên đang làm phiếu phải chuyển sang bận");
            Assert.False(world.Incidents.AssignTechnician(secondTask.TaskId, tech.TechId), "Không được gán người đang bận");
            Assert.True(world.Incidents.SuggestTechnician("HUB-08")?.TechId != tech.TechId,
                "Gợi ý kế tiếp không được là người đang bận");
        });

        runner.Add("REG-06", "Xử lý xong sự cố: thiết bị sẵn sàng lại, phiếu hoàn thành, kỹ thuật viên rảnh", () =>
        {
            var world = TestWorld.Create();
            var incident = world.Incidents.ReceiveStudentReport(
                "SV-2414064", IncidentObjectType.Vehicle, "XE-112", "Lỗi phanh", "Phanh sau bị kẹt.");
            world.Incidents.LockDevice(incident.IncidentId);
            var task = world.Incidents.CreateMaintenanceTask(incident.IncidentId);
            var tech = world.Incidents.SuggestTechnician(incident.HubId)!;
            world.Incidents.AssignTechnician(task.TaskId, tech.TechId);

            world.Incidents.ResolveIncident(incident.IncidentId, "Thay má phanh.");

            Assert.Equal(IncidentStatus.Resolved, incident.Status);
            Assert.Equal(DeviceStatus.Available, world.Store.FindVehicle("XE-112")!.Status, "Xe phải sẵn sàng lại");
            Assert.Equal(MaintenanceStatus.Completed, task.Status, "Phiếu phải hoàn thành");
            Assert.True(task.CompletedAt is not null, "Phải ghi thời điểm hoàn thành");
            Assert.True(tech.IsAvailable(), "Kỹ thuật viên phải rảnh lại");
        });

        runner.Add("REG-07", "Cổng lỗi, hết cổng trống: yêu cầu vẫn giữ đầu hàng chờ sau chu kỳ lập lịch kế tiếp", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-07");
            var onlyPort = hub.ChargingPoints[0];
            onlyPort.Status = DeviceStatus.Charging;
            onlyPort.AssignedVehicleCode = "XE-121";
            foreach (var port in hub.ChargingPoints.Skip(1)) port.Status = DeviceStatus.Faulty;

            // Yêu cầu khác điểm cao hơn (pin thấp, cần gấp) đang chờ sẵn.
            TestWorld.AddRequest(world, "CR-A", "XE-115", hub.Id, 5, 10);
            var waiting = TestWorld.AddRequest(world, "CR-Q", "XE-121", hub.Id, 90, 600);
            waiting.AssignedPortId = onlyPort.Id;

            world.Charging.HandleFaultyChargingPoint(onlyPort);
            world.Charging.RunSchedule(hub);

            Assert.Equal(0, waiting.QueuePosition, "Xe bị gián đoạn sạc phải giữ đầu hàng chờ");
        });

        runner.Add("REG-08", "Mở lại Hub sau khi tạm dừng", () =>
        {
            var world = TestWorld.Create();
            world.Incidents.SuspendHub("HUB-08", "OP-01");
            Assert.Equal(HubStatus.Suspended, world.Store.GetHub("HUB-08").Status);

            Assert.True(world.Incidents.ResumeHub("HUB-08", "OP-01"), "Hub đang tạm dừng phải mở lại được");
            Assert.Equal(HubStatus.Online, world.Store.GetHub("HUB-08").Status);
            Assert.False(world.Incidents.ResumeHub("HUB-08", "OP-01"), "Hub không tạm dừng thì không có gì để mở");
        });

        runner.Add("REG-09", "Xe giữ RESERVED sau lỗi mở khoá được trả về AVAILABLE khi sự cố xử lý xong", () =>
        {
            var world = TestWorld.Create();
            var incident = world.Incidents.ReceiveUnlockTimeout("XE-121", "R-103");
            Assert.Equal(DeviceStatus.Reserved, world.Store.FindVehicle("XE-121")!.Status, "Ngay sau lỗi: giữ RESERVED");

            var task = world.Incidents.CreateMaintenanceTask(incident.IncidentId);
            var tech = world.Incidents.SuggestTechnician(incident.HubId)!;
            world.Incidents.AssignTechnician(task.TaskId, tech.TechId);
            world.Incidents.ResolveIncident(incident.IncidentId, "Khoá hoạt động bình thường.");

            Assert.Equal(DeviceStatus.Available, world.Store.FindVehicle("XE-121")!.Status,
                "Xử lý xong thì xe không được kẹt RESERVED");
        });

        runner.Add("REG-10", "Đang quét QR: gia hạn có giới hạn, quá số lần thì huỷ", () =>
        {
            var world = TestWorld.Create();
            var reservation = world.Store.Reservations.First(r => r.Id == "R-103");

            for (var i = 0; i < ServiceConstants.MaxPickupExtensions; i++)
            {
                world.AutoCancel.AutoCancelExpiredReservations();
                Assert.Equal(ReservationStatus.PendingPickup, reservation.Status, $"Lần gia hạn {i + 1} chưa được huỷ");
                world.Clock.AdvanceMinutes(1);
            }

            Assert.Equal(ServiceConstants.MaxPickupExtensions, reservation.ExtensionCount, "Đếm đúng số lần gia hạn");
            world.AutoCancel.AutoCancelExpiredReservations();
            Assert.Equal(ReservationStatus.Cancelled, reservation.Status, "Hết lượt gia hạn thì phải huỷ");
            Assert.Equal(DeviceStatus.Available, world.Store.FindVehicle("XE-121")!.Status, "Xe trở lại sẵn sàng");
        });

        runner.Add("REG-11", "Lệnh cấp nguồn timeout không đánh dấu cổng FAULTY, thử cổng kế tiếp", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-05");
            var first = hub.ChargingPoints[0];
            world.Gateway.FailNext(first.Id, FaultKind.Timeout);
            var request = TestWorld.AddRequest(world, "CR-T", "XE-115", hub.Id, 20, 30);

            world.Charging.RunSchedule(hub);

            Assert.Equal(DeviceStatus.Available, first.Status, "Timeout chỉ là mất phản hồi tạm thời, cổng vẫn sẵn sàng");
            Assert.Equal(hub.ChargingPoints[1].Id, request.AssignedPortId, "Yêu cầu được cấp cổng kế tiếp");
            Assert.False(world.Notification.OperatorAlerts.Any(a => a.Type == AlertType.ChargerFault),
                "Không được báo cổng lỗi khi chỉ timeout");
        });

        runner.Add("REG-12", "Kiểm tra công suất tính cả các cổng đang sạc", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-08"); // 5 cổng đang sạc = 35 kW, giới hạn 56 kW
            for (var i = 0; i < 4; i++) TestWorld.AddRequest(world, $"CR-{i}", "XE-204", hub.Id, 30, 60);

            var plan = world.Charging.RunSchedule(hub);

            Assert.Equal(28, plan.TotalPowerKw, "Hàng chờ cần 4 × 7 kW");
            Assert.False(world.Charging.CheckPowerLimit(hub, plan), "35 kW đang sạc + 28 kW = 63 kW > 56 kW");
            Assert.True(world.Notification.OperatorAlerts.Any(a => a.Type == AlertType.PowerLimit && a.HubId == "HUB-08"),
                "Phải báo vượt công suất");
        });

        runner.Add("REG-13", "Vượt công suất là loại cảnh báo riêng, không bị coi là quá tải chỗ đỗ", () =>
        {
            var world = TestWorld.Create();
            var incident = world.Incidents.ReceiveSensorAlert("HUB-05", "HUB-05", AlertType.PowerLimit);
            Assert.Equal(PriorityLevel.High, incident.Priority, "Vượt công suất là HIGH, quá tải chỗ đỗ mới là CRITICAL");
        });

        runner.Add("REG-15", "Nhật ký sự cố ghi trạng thái bằng tiếng Việt, không lộ tên enum (Locked, InRepair…)", () =>
        {
            var world = TestWorld.Create();
            Assert.True(world.Incidents.LockDevice("SC-1017"), "Khoá được xe XE-087");
            var logs = world.Store.GetIncident("SC-1017").Logs.Select(l => l.Message).ToList();
            Assert.True(logs.Any(m => m.Contains("“Đã khoá”")), "Nhật ký có dòng chuyển sang “Đã khoá”");
            Assert.False(logs.Any(m => m.Contains("Locked") || m.Contains("UnderCheck") || m.Contains("InRepair")),
                "Không còn tên enum tiếng Anh trong nhật ký");
        });

        runner.Add("REG-14", "Snapshot tìm xe không phân biệt hoa thường như NetworkStore", () =>
        {
            var world = TestWorld.Create();
            var snapshot = world.Store.Snapshot();
            Assert.True(snapshot.FindVehicle(" xe-112 ") is not null, "Snapshot phải tìm được xe như store");
        });
        runner.Add("REG-16", "What-if dùng đồng hồ mô phỏng: phiên, lần chạy và khung mặc định theo giờ demo, không theo giờ máy", () =>
        {
            var world = TestWorld.Create();
            world.Clock.AdvanceMinutes(15);
            var controller = new WhatIfController(new TempStateRepository(world.Store, world.Clock),
                new WhatIfEngineService(world.Clock), world.Store.Snapshot);

            var parameters = controller.GetDefaultConfig("PARKING_FULL", "HUB-08");
            Assert.Equal(world.Clock.Now.Date, parameters.StartTime.Date, "Khung mặc định nằm trong ngày mô phỏng");

            var session = controller.ConfigureScenario("PARKING_FULL", parameters);
            Assert.Equal(world.Clock.Now, session.CreatedAt, "Phiên ghi giờ mô phỏng");

            var result = controller.StartSimulation(session.SessionId);
            Assert.Equal(world.Clock.Now, result.StartedAt, "Lần chạy ghi giờ mô phỏng");
        });

        runner.Add("REG-17", "Timer bắt buộc có scheduler; Start/Stop gắn và gỡ đúng lịch, không chạy luồng nền", () =>
        {
            var world = TestWorld.Create();
            Assert.Throws<ArgumentNullException>(() => new StatusUpdaterTimer(world.Store, world.Clock, world.Sync,
                    world.Threshold, world.Queue, world.AutoCancel, world.Charging, world.Logger, null!),
                "Thiếu scheduler phải báo lỗi ngay, không lặng lẽ dùng timer nền");

            Assert.False(world.Scheduler.Fire(), "Chưa Start thì chưa có lịch");
            world.Timer.Start();
            world.Timer.Start();
            Assert.True(world.Scheduler.Fire(), "Start xong thì lịch chạy được một chu kỳ");
            Assert.Equal(world.Clock.Now, world.Timer.LastTickAt, "Chu kỳ quét đã chạy");

            world.Timer.Stop();
            Assert.False(world.Scheduler.IsScheduled, "Stop gỡ lịch");
        });
    }

    private static int CountAlerts(TestWorld world, AlertType type, string hubId, string? contains = null) =>
        world.Notification.OperatorAlerts.Count(a =>
            a.Type == type && a.HubId == hubId && (contains is null || a.Message.Contains(contains)));
}
