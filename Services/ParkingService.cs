using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class ParkingService
    {
        private readonly StudentContext Ctx;

        public ParkingService(StudentContext ctx) => Ctx = ctx;

        private MockDataStore Store => Ctx.Store;

        public ParkingReservation? ActiveParking => Store.ParkingReservations.FirstOrDefault(p => p.State == ReservationState.Active);

        public OpResult<ParkingReservation> ReserveParking(string hubId, string vehicleId, DateTime arrival, int durationMinutes, bool alternativeHub = false)
        {
            var hub = Store.GetHub(hubId);
            if (Store.ParkingReservations.Count(p => p.State == ReservationState.Active) >= DemoConfig.MaxActiveParking)
            {
                var a = ActiveParking!;
                return OpResult<ParkingReservation>.Fail("LimitReached", $"Bạn đã có 1 đặt chỗ đỗ xe đang hoạt động ({a.SlotNumber} · {Store.GetHub(a.HubId).ShortName}). Hãy hủy hoặc hoàn tất đặt chỗ đó trước.");
            }
            if (hub.IsSuspended)
                return OpResult<ParkingReservation>.Fail("Full", $"{hub.Name} đang tạm dừng hoạt động theo quyết định của người vận hành.");
            if (arrival < Ctx.Clock.Now.AddMinutes(-1))
                return OpResult<ParkingReservation>.Fail("InPast", "Giờ đến đã qua. Vui lòng chọn giờ khác hoặc bật \"Đến ngay\".");
            if (hub.FreeSlots <= 0 || (Ctx.Toggles.ParkingHubFull && !alternativeHub))
                return OpResult<ParkingReservation>.Fail("Full", $"{hub.Name} không còn chỗ trống trong khung {DemoConfig.Time(arrival)}–{DemoConfig.Time(arrival.AddMinutes(durationMinutes))}.");

            var p = new ParkingReservation
            {
                Id = Store.NextId("PR"),
                PrivateVehicleId = vehicleId,
                HubId = hubId,
                SlotNumber = $"P-{Random.Shared.Next(10, 30)}",
                Start = arrival,
                DurationMinutes = durationMinutes,
                EntryCode = $"PK-{Random.Shared.Next(1000, 9999)}",
                State = ReservationState.Active
            };
            Store.ParkingReservations.Add(p);
            // Chỗ đỗ của xe cá nhân tính vào bộ đếm của Hub trên mạng lưới (Operator thấy mức sử dụng thật).
            Ctx.Gateway.ChangeParkingOccupancy(hubId, 1);
            Ctx.Notifications.Send("Đặt chỗ đỗ xe thành công", $"Chỗ đỗ {p.SlotNumber} tại {hub.ShortName}, {DemoConfig.Time(p.Start)}–{DemoConfig.Time(p.End)} ngày {p.Start:dd/MM}.", Tone.Success);
            return OpResult<ParkingReservation>.Success(p);
        }

        /// <summary>
        /// Đặt chỗ đỗ đã qua giờ kết thúc thì hoàn tất và trả chỗ cho Hub.
        /// Trước đây đặt chỗ ở trạng thái Active mãi: chỗ đỗ không bao giờ được trả và giới hạn
        /// 1 đặt chỗ đỗ đang hoạt động chặn sinh viên đặt chỗ mới.
        /// </summary>
        public int CompleteFinished()
        {
            var now = Ctx.Clock.Now;
            var finished = Store.ParkingReservations.Where(p => p.State == ReservationState.Active && now >= p.End).ToList();
            foreach (var p in finished)
            {
                p.State = ReservationState.Completed;
                p.ClosedAt = p.End;
                var hub = Store.GetHub(p.HubId);
                Ctx.Gateway.ChangeParkingOccupancy(p.HubId, -1);
                Ctx.Notifications.Send("Đặt chỗ đỗ đã kết thúc", $"Chỗ đỗ {p.SlotNumber} tại {hub.ShortName} đã hết giờ đặt ({DemoConfig.Time(p.End)}).", Tone.Info);
            }
            return finished.Count;
        }

        public List<Hub> FindAvailableHubsWithin1Km(string hubId)
        {
            var hub = Store.GetHub(hubId);
            return Store.Hubs.Where(h => h.Id != hubId && !h.IsSuspended && h.FreeSlots > 0 && h.DistanceKmTo(hub) <= DemoConfig.AlternativeHubRadiusKm)
                .OrderBy(h => h.DistanceKmTo(hub)).ToList();
        }
    }
}
