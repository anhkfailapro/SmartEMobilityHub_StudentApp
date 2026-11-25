using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class TripBillingService
    {
        private static MockDataStore Store => DemoApp.Store;

        public Trip? ActiveTrip => Store.ActiveTrip;

        public double ElapsedMinutes(Trip t) => ((t.EndTime ?? DemoApp.Clock.Now) - t.StartTime).TotalMinutes;

        public int CurrentBattery(Trip t) => Math.Max(3, t.StartBattery - (int)(ElapsedMinutes(t) / 3));

        public double CurrentDistanceKm(Trip t) => Math.Round(ElapsedMinutes(t) * 0.17, 1);

        public int BillableMinutes(Trip t) => Math.Max(1, (int)Math.Ceiling(ElapsedMinutes(t)));

        public int CurrentFee(Trip t) => BillableMinutes(t) * DemoConfig.RentalPricePerMinute;

        public Hub? NearestHubWithFreeSlots(Hub from, string? excludeId) =>
            Store.Hubs.Where(h => h.FreeSlots > 0 && !h.IsSuspended && h.Id != excludeId).OrderBy(h => h.DistanceKmTo(from)).FirstOrDefault();

        public OpResult<Hub> CheckReturn(string hubId, bool redirected = false)
        {
            var hub = Store.GetHub(hubId);
            if (!redirected && DemoApp.Toggles.VehicleOutsideHub)
            {
                var nearest = NearestHubWithFreeSlots(hub, null);
                return OpResult<Hub>.Fail("OutsideHub", "Xe không nằm trong khu vực Hub nên không thể trả xe. Hãy di chuyển đến Hub gần nhất còn chỗ trống.", nearest);
            }
            if (hub.IsSuspended)
                return OpResult<Hub>.Fail("HubFull", $"{hub.Name} đang tạm dừng hoạt động.", NearestHubWithFreeSlots(hub, hub.Id));
            if ((!redirected && DemoApp.Toggles.ReturnHubFull) || hub.FreeSlots <= 0)
            {
                var nearest = NearestHubWithFreeSlots(hub, hub.Id);
                var t = ActiveTrip;
                if (t != null && t.ExtraFreeMinutes == 0) t.ExtraFreeMinutes = DemoConfig.ReturnExtraFreeMinutes;
                return OpResult<Hub>.Fail("HubFull", $"{hub.Name} đã hết chỗ trống.", nearest);
            }
            return OpResult<Hub>.Success(hub);
        }

        public Trip Complete(string hubId)
        {
            var t = ActiveTrip!;
            var now = DemoApp.Clock.Now;
            var hub = Store.GetHub(hubId);
            var v = Store.GetVehicle(t.VehicleCode)!;
            var rate = DemoConfig.RentalPricePerMinute;

            var minutes = BillableMinutes(t);
            var free = Math.Min(t.ExtraFreeMinutes, minutes);
            var distance = CurrentDistanceKm(t);
            var rental = minutes * rate + (int)(distance * DemoConfig.DistanceFeePerKm);
            var planMinutes = Math.Min(Store.PlanFreeMinutes, minutes - free);
            Store.PlanFreeMinutes -= planMinutes;
            var planDeduction = planMinutes * rate;
            var total = Math.Max(0, rental - free * rate - planDeduction);

            v.Battery = CurrentBattery(t);
            t.DistanceKm = distance;
            t.EndTime = now;
            t.EndHubId = hubId;
            t.EndSlot = $"B-{hub.ParkingCapacity - hub.FreeSlots + 1:00}";
            t.Invoice = new Invoice
            {
                Number = Store.NextInvoiceNumber(),
                Minutes = minutes,
                FreeExtraMinutes = free,
                RentalFee = rental,
                PlanDeduction = planDeduction,
                Total = total,
                PaidByPlanOnly = total == 0,
                PaymentMethod = DemoConfig.PaymentMethod
            };

            v.Status = v.Battery < DemoConfig.NeedsChargeBelow ? VehicleStatus.NeedsCharge : VehicleStatus.Available;
            v.HubId = hubId;
            v.Slot = t.EndSlot;
            v.LastUpdate = now;
            hub.FreeSlots = Math.Max(0, hub.FreeSlots - 1);
            DemoApp.Link.OnReturned(v, hubId);

            var r = Store.VehicleReservations.FirstOrDefault(x => x.State == ReservationState.Started);
            if (r != null)
            {
                r.State = ReservationState.Completed;
                r.ClosedAt = now;
            }
            Store.ActiveTrip = null;

            if (total == 0)
                DemoApp.Notifications.Send("Đã trừ vào gói tháng", $"Hóa đơn #{t.Invoice.Number} · Chuyến {minutes} phút được trừ vào phút miễn phí của gói tháng.", Tone.Success);
            else
                DemoApp.Notifications.Send("Thanh toán thành công", $"Hóa đơn #{t.Invoice.Number} · {DemoConfig.Money(total)} · Chuyến {minutes} phút.", Tone.Success);
            return t;
        }
    }
}
