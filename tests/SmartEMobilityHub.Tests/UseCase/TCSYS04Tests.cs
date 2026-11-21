using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Tests.Support;

namespace SmartEMobilityHub.Tests.UseCase;

/// <summary>UC-SYS-04 — Bảng 65 (TCSYS04-01..03).</summary>
public static class TCSYS04Tests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("TCSYS04-01", "Đặt chỗ quá 15 phút bị huỷ và trả tài nguyên", () =>
        {
            var world = TestWorld.Create();

            var cancelled = world.AutoCancel.AutoCancelExpiredReservations();

            Assert.Equal(1, cancelled, "Chỉ R-101 quá hạn và không nhận xe");
            var reservation = world.Store.Reservations.First(r => r.Id == "R-101");
            Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
            Assert.Equal(DeviceStatus.Available, world.Store.FindVehicle("XE-105")!.Status, "Xe phải trở lại AVAILABLE");
            Assert.True(
                world.Notification.UserMessages.Any(m => m.UserId == "SV-2414064"),
                "Người dùng phải nhận thông báo huỷ");
        });

        runner.Add("TCSYS04-02", "Đặt chỗ 14 phút chưa bị huỷ (biên 15 phút)", () =>
        {
            var world = TestWorld.Create();
            var reservation = world.Store.Reservations.First(r => r.Id == "R-102");
            var holdUntil = reservation.HoldUntil;

            world.AutoCancel.AutoCancelExpiredReservations();

            Assert.Equal(ReservationStatus.PendingPickup, reservation.Status, "R-102 phải giữ nguyên");
            Assert.Equal(holdUntil, reservation.HoldUntil);
        });

        runner.Add("TCSYS04-03", "Người dùng đang quét QR được gia hạn, không bị huỷ", () =>
        {
            var world = TestWorld.Create();
            var reservation = world.Store.Reservations.First(r => r.Id == "R-103");

            world.AutoCancel.AutoCancelExpiredReservations();

            Assert.Equal(ReservationStatus.PendingPickup, reservation.Status, "R-103 không được huỷ");
            Assert.True(reservation.HoldUntil > world.Clock.Now, "HoldUntil phải được gia hạn");

            // Hoàn tất mở khoá: xe sang IN_USE, chu kỳ sau không còn huỷ.
            world.Reservations.MarkPickedUp("R-103");
            Assert.Equal(DeviceStatus.InUse, world.Store.FindVehicle("XE-121")!.Status);

            world.Clock.AdvanceMinutes(20);
            world.AutoCancel.AutoCancelExpiredReservations();

            Assert.Equal(ReservationStatus.PickedUp, reservation.Status, "Đơn đã nhận xe không bị huỷ nữa");
        });
    }
}
