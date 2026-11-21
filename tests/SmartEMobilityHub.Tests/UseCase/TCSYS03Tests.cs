using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.UseCase;

/// <summary>UC-SYS-03 — Bảng 64 (TCSYS03-01..03).</summary>
public static class TCSYS03Tests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("TCSYS03-01", "Hub 54/60 (90%) sinh cảnh báo quá tải", () =>
        {
            var world = TestWorld.Create();
            var alerts = world.Threshold.CheckAllHubs();

            Assert.Equal(1, alerts.Count, "Chỉ HUB-08 chạm ngưỡng 90%");
            var alert = alerts[0];
            Assert.Equal(AlertType.HubOverload, alert.Type);
            Assert.Equal("HUB-08", alert.HubId);

            Assert.True(world.Queue.DispatchAlert(alert), "Gửi cảnh báo phải thành công");
            Assert.True(alert.Delivered, "Cảnh báo phải được đánh dấu đã gửi");
            Assert.True(world.Notification.OperatorAlerts.Contains(alert), "Operator phải nhận được cảnh báo");
        });

        runner.Add("TCSYS03-02", "Hub 53/60 (88,3%) không sinh cảnh báo", () =>
        {
            var world = TestWorld.Create();
            world.Store.GetHub("HUB-08").OccupiedParkingSlots = 53;

            var alerts = world.Threshold.CheckAllHubs();

            Assert.Equal(0, alerts.Count, "Dưới ngưỡng 90% thì không cảnh báo");
        });

        runner.Add("TCSYS03-03", "Gửi cảnh báo lỗi mạng được xếp hàng và gửi lại sau 30 giây", () =>
        {
            var world = TestWorld.Create();
            world.Notification.NetworkDown = true;

            var alert = new Alert
            {
                AlertId = "AL-TEST-1",
                Type = AlertType.HubOverload,
                HubId = "HUB-08",
                Message = "HUB-08 quá tải",
                CreatedAt = world.Clock.Now
            };

            Assert.False(world.Queue.DispatchAlert(alert), "Gửi lúc mất mạng phải thất bại");
            Assert.Equal(1, world.Queue.Size(), "Alert phải nằm trong hàng đợi");

            world.Clock.AdvanceSeconds(29);
            Assert.Equal(0, world.Queue.ProcessQueue(world.Clock.Now), "Chưa đủ 30 giây thì chưa gửi lại");
            Assert.Equal(1, world.Queue.Size());

            world.Notification.NetworkDown = false;
            world.Clock.AdvanceSeconds(1);
            Assert.Equal(1, world.Queue.ProcessQueue(world.Clock.Now), "Đủ 30 giây phải gửi lại thành công");
            Assert.Equal(0, world.Queue.Size(), "Hàng đợi phải rỗng sau khi gửi");
            Assert.True(alert.Delivered, "Cảnh báo phải được đánh dấu đã gửi sau lần thử lại");
        });
    }
}
