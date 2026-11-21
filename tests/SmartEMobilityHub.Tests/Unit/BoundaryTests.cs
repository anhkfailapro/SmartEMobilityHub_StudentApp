using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.Unit;

/// <summary>Kiểm thử biên cho các quy tắc dễ sai nhất (Bảng 30/31/37/45).</summary>
public static class BoundaryTests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("UNIT-01", "Ngưỡng quá tải: 90% tính là quá tải, dưới 90% thì không", () =>
        {
            var world = TestWorld.Create();
            Assert.True(world.Threshold.IsOverloaded(54, 60), "54/60 = 90% phải quá tải");
            Assert.False(world.Threshold.IsOverloaded(53, 60), "53/60 = 88,3% chưa quá tải");
            Assert.False(world.Threshold.IsOverloaded(0, 0), "Sức chứa 0 không được chia cho 0");
            Assert.False(world.Threshold.IsOverloaded(0, 60), "0/60 chưa quá tải");
        });

        runner.Add("UNIT-02", "Ngưỡng cấu hình phải trong (0, 1]", () =>
        {
            var world = TestWorld.Create();
            Assert.Throws<IllegalArgumentException>(() => world.Threshold.SetThreshold(0), "0 không hợp lệ");
            Assert.Throws<IllegalArgumentException>(() => world.Threshold.SetThreshold(1.2), "1,2 không hợp lệ");

            world.Threshold.SetThreshold(0.8);
            Assert.True(world.Threshold.IsOverloaded(48, 60), "48/60 = 80% chạm ngưỡng mới");
            Assert.False(world.Threshold.IsOverloaded(47, 60), "47/60 = 78,3% chưa chạm ngưỡng 80%");
        });

        runner.Add("UNIT-03", "Dữ liệu cũ: đúng 10 phút chưa cảnh báo, quá 1 giây thì cảnh báo", () =>
        {
            var world = TestWorld.Create();
            var hub = world.Store.GetHub("HUB-01");

            hub.LastUpdatedAt = world.Clock.Now.AddMinutes(-10);
            Assert.False(world.Timer.CheckStaleData(hub), "Đúng 10 phút chưa phải dữ liệu cũ");

            hub.LastUpdatedAt = world.Clock.Now.AddMinutes(-10).AddSeconds(-1);
            Assert.True(world.Timer.CheckStaleData(hub), "Quá 10 phút 1 giây phải cảnh báo");
        });

        runner.Add("UNIT-04", "Điểm ưu tiên: pin 0% + cần ngay = 92,5; pin 100% + còn xa = 7,5", () =>
        {
            var world = TestWorld.Create();
            var urgent = TestWorld.AddRequest(world, "CR-U", "XE-115", "HUB-05", 0, 0);
            var relaxed = TestWorld.AddRequest(world, "CR-R", "XE-116", "HUB-05", 100, 300);

            Assert.InRange(world.Charging.CalculatePriorityScore(urgent), 92.49, 92.51, "Cận trên");
            Assert.InRange(world.Charging.CalculatePriorityScore(relaxed), 7.49, 7.51, "Cận dưới");

            Assert.Throws<IllegalArgumentException>(
                () => world.Charging.CalculatePriorityScore(null!),
                "Yêu cầu null phải bị từ chối");

            var invalid = TestWorld.AddRequest(world, "CR-I", "XE-112", "HUB-05", 101, 30);
            Assert.Throws<IllegalArgumentException>(
                () => world.Charging.CalculatePriorityScore(invalid),
                "Pin 101% phải bị từ chối");
        });

        runner.Add("UNIT-05", "SLA: đúng hạn chưa vi phạm, quá 1 giây thì vi phạm; đã xong thì không tính", () =>
        {
            var now = TestWorld.Start;
            var task = new MaintenanceTask
            {
                TaskId = "MT-TEST",
                IncidentId = "SC-TEST",
                Status = MaintenanceStatus.Assigned,
                SlaDeadline = now
            };

            Assert.False(task.IsSlaBreached(now), "Đúng hạn chưa vi phạm");
            Assert.True(task.IsSlaBreached(now.AddSeconds(1)), "Quá 1 giây là vi phạm");

            task.Status = MaintenanceStatus.Completed;
            Assert.False(task.IsSlaBreached(now.AddHours(1)), "Phiếu đã xong không tính vi phạm SLA");
        });
    }
}
