using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class ParkingService
    {
        private static MockDataStore Store => DemoApp.Store;

        public ParkingReservation? ActiveParking => Store.ParkingReservations.FirstOrDefault(p => p.State == ReservationState.Active);

        public OpResult<ParkingReservation> ReserveParking(string hubId, string vehicleId, DateTime arrival, int durationMinutes, bool alternativeHub = false)
        {
            var hub = Store.GetHub(hubId);
            if (Store.ParkingReservations.Count(p => p.State == ReservationState.Active) >= DemoConfig.MaxActiveParking)
            {
                var a = ActiveParking!;
                return OpResult<ParkingReservation>.Fail("LimitReached", $"Bạn đã có 1 đặt chỗ đỗ xe đang hoạt động ({a.SlotNumber} · {Store.GetHub(a.HubId).ShortName}). Hãy hủy hoặc hoàn tất đặt chỗ đó trước.");
            }
            if (arrival < DemoApp.Clock.Now.AddMinutes(-1))
                return OpResult<ParkingReservation>.Fail("InPast", "Giờ đến đã qua. Vui lòng chọn giờ khác hoặc bật \"Đến ngay\".");
            if (hub.FreeSlots <= 0 || (DemoApp.Toggles.ParkingHubFull && !alternativeHub))
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
            hub.FreeSlots--;
            Store.ParkingReservations.Add(p);
            DemoApp.Notifications.Send("Đặt chỗ đỗ xe thành công", $"Chỗ đỗ {p.SlotNumber} tại {hub.ShortName}, {DemoConfig.Time(p.Start)}–{DemoConfig.Time(p.End)} ngày {p.Start:dd/MM}.", Tone.Success);
            return OpResult<ParkingReservation>.Success(p);
        }

        public List<Hub> FindAvailableHubsWithin1Km(string hubId)
        {
            var hub = Store.GetHub(hubId);
            return Store.Hubs.Where(h => h.Id != hubId && h.FreeSlots > 0 && h.DistanceKmTo(hub) <= DemoConfig.AlternativeHubRadiusKm)
                .OrderBy(h => h.DistanceKmTo(hub)).ToList();
        }
    }
}
