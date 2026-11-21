using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.UseCase;

/// <summary>UC-OP-03 — Bảng 67 (TCOP03-01..05).</summary>
public static class TCOP03Tests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("TCOP03-01", "Tiếp nhận báo cáo sự cố từ sinh viên", () =>
        {
            var world = TestWorld.Create();
            var before = world.Store.Incidents.Count;

            var record = world.Incidents.ReceiveStudentReport(
                "SV-2414064",
                IncidentObjectType.Vehicle,
                "XE-112",
                "Lỗi bộ điều khiển",
                "Xe không nhận ga, đèn báo lỗi nhấp nháy.",
                2L * 1024 * 1024);

            Assert.Equal(before + 1, world.Store.Incidents.Count, "Phải tạo mới một sự cố");
            Assert.Equal(IncidentStatus.New, record.Status);
            Assert.Equal(IncidentSource.StudentReport, record.Source);
            Assert.Equal("SV-2414064", record.ReporterIds[0]);
            Assert.True(record.IncidentId.StartsWith("SC-"), "Mã sự cố phải có tiền tố SC-");
            Assert.Equal(DeviceStatus.UnderCheck, world.Store.FindVehicle("XE-112")!.Status, "Xe phải sang UNDER_CHECK");
            Assert.True(world.Incidents.GetIncidentQueue().Any(i => i.IncidentId == record.IncidentId), "Sự cố mới phải có trong hàng đợi");
            Assert.True(world.Notification.UserMessages.Any(m => m.UserId == "SV-2414064"), "Người báo phải nhận phản hồi");

            // Mất mạng thông báo không được làm hỏng việc tiếp nhận sự cố.
            var offlineWorld = TestWorld.Create();
            offlineWorld.Notification.NetworkDown = true;
            var beforeOffline = offlineWorld.Store.Incidents.Count;
            var offlineRecord = offlineWorld.Incidents.ReceiveStudentReport(
                "SV-2414066",
                IncidentObjectType.Vehicle,
                "XE-114",
                "Đèn pha chập chờn",
                "Đèn pha lúc sáng lúc tắt.",
                0);

            Assert.True(
                offlineWorld.Store.Incidents.Count == beforeOffline + 1 &&
                offlineRecord.Status == IncidentStatus.New &&
                offlineWorld.Store.FindVehicle("XE-114")!.Status == DeviceStatus.UnderCheck &&
                offlineWorld.Logger.Contains("WARN", "không gửi được thông báo"),
                "Mất mạng thông báo: vẫn tạo sự cố, xe sang UNDER_CHECK và chỉ ghi WARN");
        });

        runner.Add("TCOP03-02", "Báo cáo trùng được gộp vào sự cố đang mở", () =>
        {
            var world = TestWorld.Create();
            var before = world.Store.Incidents.Count;
            var existing = world.Store.GetIncident("SC-1023");
            var reporters = existing.ReporterIds.Count;

            var record = world.Incidents.ReceiveStudentReport(
                "SV-2414065",
                IncidentObjectType.Vehicle,
                "XE-204",
                "Lỗi bộ điều khiển",
                "Xe vẫn không nhận ga.",
                1L * 1024 * 1024);

            Assert.Equal("SC-1023", record.IncidentId, "Phải gộp vào SC-1023");
            Assert.Equal(before, world.Store.Incidents.Count, "Không được tạo sự cố mới");
            Assert.Equal(reporters + 1, record.ReporterIds.Count, "Phải ghi thêm người báo");

            Assert.Throws<FileTooLargeException>(
                () => world.Incidents.ReceiveStudentReport(
                    "SV-2414065",
                    IncidentObjectType.Vehicle,
                    "XE-112",
                    "Ảnh quá lớn",
                    "Ảnh 6 MB",
                    6L * 1024 * 1024),
                "Ảnh vượt 5 MB phải bị từ chối");
        });

        runner.Add("TCOP03-03", "Khoá thiết bị, tạo phiếu bảo trì và gán kỹ thuật viên", () =>
        {
            var world = TestWorld.Create();

            Assert.True(world.Incidents.LockDevice("SC-1017"), "Khoá được xe XE-087");
            Assert.Equal(DeviceStatus.Locked, world.Store.FindVehicle("XE-087")!.Status);
            Assert.Equal(IncidentStatus.Locked, world.Store.GetIncident("SC-1017").Status);

            var task = world.Incidents.CreateMaintenanceTask("SC-1017");
            Assert.Equal(MaintenanceStatus.Assigned, task.Status);
            Assert.Equal(TimeSpan.FromHours(8), task.GetRemainingSla(world.Clock.Now), "SLA mức Medium là 8 giờ");

            Assert.True(world.Incidents.AssignTechnician(task.TaskId, "T-02"), "T-02 đang sẵn sàng");
            Assert.Equal("T-02", task.AssignedTech!.TechId);
            Assert.Equal(IncidentStatus.InRepair, world.Store.GetIncident("SC-1017").Status);
            Assert.Equal(DeviceStatus.UnderRepair, world.Store.FindVehicle("XE-087")!.Status);

            Assert.False(world.Incidents.AssignTechnician(task.TaskId, "T-03"), "T-03 đang bận, phải từ chối");
            Assert.True(task.AssignedTech!.TechId == "T-02", "Không được đổi kỹ thuật viên");
        });

        runner.Add("TCOP03-04", "Tạm dừng Hub huỷ đúng các đặt chỗ sắp tới", () =>
        {
            var world = TestWorld.Create();

            var cancelled = world.Incidents.SuspendHub("HUB-08", "OP-01");

            Assert.Equal(5, cancelled, "HUB-08 có 5 đặt chỗ sắp tới");
            Assert.Equal(HubStatus.Suspended, world.Store.GetHub("HUB-08").Status);
            Assert.Equal(5, world.Store.Reservations.Count(r => r.HubId == "HUB-08" && r.Status == ReservationStatus.Cancelled));
            Assert.True(world.Notification.Broadcasts.Any(b => b.HubId == "HUB-08"), "Phải phát thông báo cho người bị ảnh hưởng");

            // Mất mạng broadcast không được làm sai kết quả tạm dừng Hub.
            var offlineWorld = TestWorld.Create();
            offlineWorld.Notification.NetworkDown = true;
            var offlineCancelled = offlineWorld.Incidents.SuspendHub("HUB-08", "OP-01");

            Assert.True(
                offlineCancelled == 5 &&
                offlineWorld.Store.Reservations.Count(r => r.HubId == "HUB-08" && r.Status == ReservationStatus.Cancelled) == 5 &&
                offlineWorld.Logger.Contains("WARN", "không gửi được thông báo hàng loạt"),
                "Mất mạng broadcast: vẫn huỷ đủ 5 đơn, trả về 5 và chỉ ghi WARN");

            Assert.Throws<HubNotFoundException>(
                () => world.Incidents.SuspendHub("HUB-99", "OP-01"),
                "Hub không tồn tại phải báo lỗi");
        });

        runner.Add("TCOP03-05", "Mở khoá quá 30 giây không xác nhận", () =>
        {
            var world = TestWorld.Create();

            var record = world.Incidents.ReceiveUnlockTimeout("XE-121", "R-103");

            Assert.Equal(IncidentSource.UnlockTimeout, record.Source);
            Assert.Equal(DeviceStatus.Reserved, world.Store.FindVehicle("XE-121")!.Status, "Xe phải giữ RESERVED");
            Assert.Equal(ReservationStatus.Cancelled, world.Store.Reservations.First(r => r.Id == "R-103").Status);
            Assert.True(record.Description.Contains("1900"), "Phải hiển thị hotline hỗ trợ");
            Assert.True(
                world.Notification.UserMessages.Any(m => m.UserId == "SV-2414066" && m.Message.Contains("1900")),
                "Người dùng phải nhận hotline");
        });
    }
}
