using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class PickUpService
    {
        private static MockDataStore Store => DemoApp.Store;

        public OpResult<VehicleReservation> VerifyPickUpCode(string vehicleCode, string? pickUpCode)
        {
            var r = DemoApp.Reservations.ActiveVehicleReservation;
            if (r == null)
            {
                var last = Store.VehicleReservations.LastOrDefault();
                if (last != null && last.State == ReservationState.Expired)
                    return OpResult<VehicleReservation>.Fail("Expired", "Hết thời gian giữ xe. Đặt chỗ đã được giải phóng.");
                return OpResult<VehicleReservation>.Fail("NoReservation", "Bạn không có đặt xe đang hoạt động.");
            }
            var hub = Store.GetHub(r.HubId);
            if (DemoApp.Toggles.NotAtHub)
                return OpResult<VehicleReservation>.Fail("NotAtHub", $"Bạn cần có mặt tại {hub.Name} để nhận xe.", r);
            if (DemoApp.Clock.Now > r.HoldUntil && !r.IsPickingUp)
            {
                DemoApp.Reservations.AutoCancelExpired();
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
            r.IsPickingUp = true;
            return OpResult<VehicleReservation>.Success(r);
        }

        private static string Normalize(string code) => code.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        public Trip ConfirmStart(VehicleReservation r)
        {
            var now = DemoApp.Clock.Now;
            var v = Store.GetVehicle(r.VehicleCode)!;
            var hub = Store.GetHub(r.HubId);
            v.Status = VehicleStatus.InUse;
            v.HubId = null;
            v.LastUpdate = now;
            hub.FreeSlots = Math.Min(hub.ParkingCapacity, hub.FreeSlots + 1);
            r.State = ReservationState.Started;
            r.IsPickingUp = false;
            var trip = new Trip { Id = Store.NextId("T"), VehicleCode = v.Code, StartHubId = hub.Id, StartTime = now, StartBattery = v.Battery };
            Store.Trips.Add(trip);
            Store.ActiveTrip = trip;
            return trip;
        }

        public Incident ReceiveUnlockTimeout(VehicleReservation r)
        {
            r.IsPickingUp = false;
            return DemoApp.Incidents.CreateUnlockTimeoutIncident(r.VehicleCode, r.HubId);
        }
    }
}
