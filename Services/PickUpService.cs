using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Services
{
    public class PickUpService
    {
        private readonly StudentContext Ctx;

        public PickUpService(StudentContext ctx) => Ctx = ctx;

        private MockDataStore Store => Ctx.Store;

        public OpResult<VehicleReservation> VerifyPickUpCode(string vehicleCode, string? pickUpCode)
        {
            var r = Ctx.Reservations.ActiveVehicleReservation;
            if (r == null)
            {
                var last = Store.VehicleReservations.OrderBy(x => x.CreatedAt).LastOrDefault();
                if (last != null && last.State == ReservationState.Expired)
                    return OpResult<VehicleReservation>.Fail("Expired", "Hết thời gian giữ xe. Đặt chỗ đã được giải phóng.");
                if (last != null && last.State == ReservationState.Cancelled && last.CancelReason != null)
                    return OpResult<VehicleReservation>.Fail("Cancelled", $"Đặt xe {last.VehicleCode} đã bị hủy: {last.CancelReason}.");
                return OpResult<VehicleReservation>.Fail("NoReservation", "Bạn không có đặt xe đang hoạt động.");
            }
            var hub = Store.GetHub(r.HubId);
            if (Ctx.Toggles.NotAtHub)
                return OpResult<VehicleReservation>.Fail("NotAtHub", $"Bạn cần có mặt tại {hub.Name} để nhận xe.", r);
            if (Ctx.Clock.Now > r.HoldUntil && !r.IsPickingUp)
            {
                Ctx.Reservations.AutoCancelExpired();
                if (r.State != ReservationState.Active)
                    return OpResult<VehicleReservation>.Fail("Expired", "Hết thời gian giữ xe. Đặt chỗ đã được giải phóng.");
            }
            if (r.WrongAttempts > DemoConfig.MaxExtraPickUpAttempts)
                return OpResult<VehicleReservation>.Fail("NoAttempts", $"Bạn đã hết lượt thử. Vui lòng liên hệ {DemoConfig.SupportHotline} ({DemoConfig.SupportHours}).", r);

            var match = string.Equals(vehicleCode.Trim(), r.VehicleCode, StringComparison.OrdinalIgnoreCase)
                        && (pickUpCode == null || Normalize(pickUpCode) == Normalize(r.PickUpCode));
            if (!match)
            {
                r.WrongAttempts++;
                var left = DemoConfig.MaxExtraPickUpAttempts + 1 - r.WrongAttempts;
                if (left <= 0)
                    return OpResult<VehicleReservation>.Fail("NoAttempts", $"Mã không khớp và bạn đã hết lượt thử. Đặt chỗ vẫn được giữ. Liên hệ {DemoConfig.SupportHotline}.", r);
                var what = pickUpCode == null ? $"Mã xe {vehicleCode.Trim()}" : "Mã xe hoặc mã nhận xe";
                return OpResult<VehicleReservation>.Fail("Wrong", $"{what} không khớp với đặt chỗ ({r.VehicleCode}). Còn {left} lần thử. Đặt chỗ vẫn được giữ.", r);
            }
            // Đang quét QR / mở khoá: mạng lưới gia hạn giữ xe có giới hạn thay vì huỷ (UC-SYS-04 5a, A10).
            Ctx.Gateway.SetPickingUp(r.Id, true);
            return OpResult<VehicleReservation>.Success(r);
        }

        private static string Normalize(string code) => code.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        /// <summary>
        /// Xe xác nhận mở khoá: chuyến đi bắt đầu. Trả null nếu mạng lưới không còn cho nhận xe
        /// (ví dụ Operator vừa tạm dừng Hub và huỷ đặt chỗ).
        /// </summary>
        public Trip? ConfirmStart(VehicleReservation r)
        {
            var now = Ctx.Clock.Now;
            var v = Store.GetVehicle(r.VehicleCode);
            if (v == null) return null;
            var startBattery = v.Battery;
            try
            {
                Ctx.Gateway.PickUp(r.Id);
            }
            catch (Exception ex) when (ex is CoreDomain.IllegalStateException or CoreDomain.IllegalArgumentException)
            {
                return null;
            }
            var trip = new Trip { Id = Store.NextId("T"), VehicleCode = v.Code, StartHubId = r.HubId, StartTime = now, StartBattery = startBattery };
            Store.Trips.Add(trip);
            Store.ActiveTrip = trip;
            return trip;
        }

        /// <summary>
        /// Không nhận được xác nhận mở khoá sau 30 giây (UC-06 4a / TCOP03-05): mạng lưới huỷ lệnh, giữ xe ở trạng thái
        /// Đã đặt và tạo sự cố cho người vận hành.
        /// </summary>
        public Incident ReceiveUnlockTimeout(VehicleReservation r)
        {
            var record = Ctx.Gateway.ReportUnlockTimeout(r.VehicleCode, r.Id);
            return Store.TrackIncident(record);
        }
    }
}
