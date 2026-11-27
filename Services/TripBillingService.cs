using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class TripBillingService
    {
        private readonly StudentContext Ctx;

        public TripBillingService(StudentContext ctx) => Ctx = ctx;

        private MockDataStore Store => Ctx.Store;

        public Trip? ActiveTrip => Store.ActiveTrip;

        public double ElapsedMinutes(Trip t) => ((t.EndTime ?? Ctx.Clock.Now) - t.StartTime).TotalMinutes;

        public int CurrentBattery(Trip t) => Math.Max(3, t.StartBattery - (int)(ElapsedMinutes(t) / 3));

        public double CurrentDistanceKm(Trip t) => Math.Round(ElapsedMinutes(t) * 0.17, 1);

        public int BillableMinutes(Trip t) => Math.Max(1, (int)Math.Ceiling(ElapsedMinutes(t)));

        public int CurrentFee(Trip t) => BillableMinutes(t) * DemoConfig.RentalPricePerMinute;

        public Hub? NearestHubWithFreeSlots(Hub from, string? excludeId) =>
            Store.Hubs.Where(h => h.FreeSlots > 0 && !h.IsSuspended && h.Id != excludeId).OrderBy(h => h.DistanceKmTo(from)).FirstOrDefault();

        public OpResult<Hub> CheckReturn(string hubId, bool redirected = false)
        {
            var hub = Store.GetHub(hubId);
            if (!redirected && Ctx.Toggles.VehicleOutsideHub)
            {
                var nearest = NearestHubWithFreeSlots(hub, null);
                return OpResult<Hub>.Fail("OutsideHub", "Xe không nằm trong khu vực Hub nên không thể trả xe. Hãy di chuyển đến Hub gần nhất còn chỗ trống.", nearest);
            }
            if (hub.IsSuspended)
                return OpResult<Hub>.Fail("HubFull", $"{hub.Name} đang tạm dừng hoạt động.", NearestHubWithFreeSlots(hub, hub.Id));
            if ((!redirected && Ctx.Toggles.ReturnHubFull) || hub.FreeSlots <= 0)
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
            var now = Ctx.Clock.Now;
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

            var battery = CurrentBattery(t);
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

            // Mạng lưới ghi nhận trả xe: xe vào Hub (chiếm một chỗ đỗ), cập nhật pin, đóng đặt chỗ đã nhận,
            // xe dưới 20% pin thành "Cần sạc"; nếu xe đã được cấp cổng sạc tại Hub này thì vào sạc luôn.
            Ctx.Gateway.ReturnVehicle(v.Code, hubId, t.EndSlot, battery, battery < DemoConfig.NeedsChargeBelow);
            Store.ActiveTrip = null;

            if (total == 0)
                Ctx.Notifications.Send("Đã trừ vào gói tháng", $"Hóa đơn #{t.Invoice.Number} · Chuyến {minutes} phút được trừ vào phút miễn phí của gói tháng.", Tone.Success);
            else
                Ctx.Notifications.Send("Thanh toán thành công", $"Hóa đơn #{t.Invoice.Number} · {DemoConfig.Money(total)} · Chuyến {minutes} phút.", Tone.Success);
            return t;
        }
    }
}
