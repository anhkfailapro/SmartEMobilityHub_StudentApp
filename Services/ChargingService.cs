using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Services
{
    /// <summary>
    /// Yêu cầu sạc xe dùng chung (UC-08). Hàng chờ, điểm ưu tiên và việc cấp cổng là của SmartChargingScheduler
    /// trong Core (DC-54): yêu cầu của sinh viên nằm ngay trong hàng chờ mà Operator đang điều phối.
    /// </summary>
    public class ChargingService
    {
        private readonly StudentContext Ctx;

        public ChargingService(StudentContext ctx) => Ctx = ctx;

        private MockDataStore Store => Ctx.Store;

        /// <summary>Thời điểm cần xe: đang đi thì 30 phút nữa, không thì 2 giờ nữa.</summary>
        private DateTime NeededAt => Store.ActiveTrip != null ? Ctx.Clock.Now.AddMinutes(30) : Ctx.Clock.Now.AddHours(2);

        public ChargingEstimate GetChargingEstimate(string vehicleCode, string hubId, bool applyBusyToggle = true)
        {
            var hub = Store.GetHub(hubId);
            var allBusy = (applyBusyToggle && Ctx.Toggles.AllChargingPointsBusy) || hub.FreePoints == 0;
            var position = hub.QueueLength + 1;
            var wait = allBusy ? 15 + position * 5 : position * 4;
            // Điểm ưu tiên tính bằng đúng công thức của Core (pin, thời điểm cần, cao điểm — UC-SYS-01).
            var probe = new CoreDomain.ChargingRequest
            {
                RequestId = "ESTIMATE",
                VehicleId = vehicleCode,
                HubId = hubId,
                BatteryLevel = Math.Clamp(CurrentBattery(vehicleCode), 0, 100),
                NeededAt = NeededAt
            };
            var score = Math.Round(Ctx.Network.Charging.CalculatePriorityScore(probe));
            return new ChargingEstimate
            {
                Hub = hub,
                FreePoints = allBusy ? 0 : hub.FreePoints,
                AllBusy = allBusy,
                WaitMinutes = wait,
                QueuePosition = position,
                Score = score,
                Priority = DemoConfig.PriorityFromScore(score)
            };
        }

        /// <summary>Pin hiện tại: xe đang trong chuyến thì tính theo quãng đã đi, không thì theo cảm biến trên Core.</summary>
        private int CurrentBattery(string vehicleCode)
        {
            var trip = Store.ActiveTrip;
            if (trip != null && string.Equals(trip.VehicleCode, vehicleCode, StringComparison.OrdinalIgnoreCase))
                return Ctx.Billing.CurrentBattery(trip);
            return Store.GetVehicle(vehicleCode)?.Battery ?? 50;
        }

        public List<ChargingEstimate> OtherHubs(string vehicleCode, string hubId) =>
            Store.Hubs.Where(h => h.Id != hubId && !h.IsSuspended && h.Points.Count > 0)
                .Select(h => GetChargingEstimate(vehicleCode, h.Id, false))
                .OrderBy(e => e.WaitMinutes).ThenBy(e => e.Hub.Id, StringComparer.Ordinal).ToList();

        public ChargingRequest? OpenRequestFor(string vehicleCode) =>
            Store.ChargingRequests.FirstOrDefault(r => string.Equals(r.VehicleCode, vehicleCode, StringComparison.OrdinalIgnoreCase)
                                                       && r.State != ChargingRequestState.Cancelled);

        public OpResult<ChargingRequest> SubmitChargingRequest(string vehicleCode, string hubId, bool applyBusyToggle = true)
        {
            var open = OpenRequestFor(vehicleCode);
            if (open != null)
            {
                var where = open.State == ChargingRequestState.Assigned ? $"cổng #{open.AssignedPoint}" : $"#{open.QueuePosition} trong hàng chờ";
                return OpResult<ChargingRequest>.Fail("Duplicate", $"{vehicleCode} đã có yêu cầu sạc đang mở tại {Store.GetHub(open.HubId).ShortName} ({where}). Không tạo yêu cầu mới.", open);
            }
            var hub = Store.GetHub(hubId);
            if (hub.IsSuspended)
                return OpResult<ChargingRequest>.Fail("HubSuspended", $"{hub.Name} đang tạm dừng hoạt động. Vui lòng chọn Hub khác.");

            // UC-08 2a (Demo panel): các cổng còn trống bị xe khác chiếm thật trên mạng lưới, Operator cũng thấy.
            if (applyBusyToggle && Ctx.Toggles.AllChargingPointsBusy) Ctx.Gateway.OccupyFreeChargingPoints(hubId);

            var battery = CurrentBattery(vehicleCode);
            CoreDomain.ChargingRequest core;
            try
            {
                core = Ctx.Gateway.RequestCharging(Store.NextId("CR"), vehicleCode, hubId, battery, NeededAt);
            }
            catch (Exception ex) when (ex is CoreDomain.IllegalArgumentException or CoreDomain.HubNotFoundException)
            {
                return OpResult<ChargingRequest>.Fail("Rejected", ex.Message);
            }
            var r = Store.Track(core, Ctx.Clock.Now);
            if (r.State == ChargingRequestState.Assigned)
                Ctx.Notifications.Send("Đã có cổng sạc", $"Cổng sạc #{r.AssignedPoint} tại {hub.ShortName} đã sẵn sàng cho {vehicleCode}.", Tone.Charging);
            else
                Ctx.Notifications.Send("Đã gửi yêu cầu sạc", $"{vehicleCode} ở vị trí #{r.QueuePosition} tại {hub.ShortName}, chờ khoảng {r.WaitMinutes} phút.", Tone.Charging);
            return OpResult<ChargingRequest>.Success(r);
        }

        /// <summary>
        /// Demo panel "Cấp cổng sạc": một xe đang sạc tại Hub sạc xong và trả cổng (nếu Hub hết cổng trống),
        /// rồi bộ lập lịch của Core cấp cổng cho yêu cầu đầu hàng chờ. Thông báo do NetworkWatcher gửi.
        /// </summary>
        public ChargingRequest? AssignNextChargingPoint()
        {
            var waiting = Store.ChargingRequests.Where(r => r.State == ChargingRequestState.InQueue).ToList();
            var assigned = Ctx.Gateway.AssignNextChargingPoint(waiting.Select(r => r.Id));
            return assigned is null ? null : waiting.FirstOrDefault(r => r.Id == assigned.RequestId);
        }

        /// <summary>Huỷ yêu cầu sạc: rời hàng chờ, hoặc trả cổng nếu đã được cấp (Core xoá yêu cầu).</summary>
        public void Cancel(ChargingRequest r)
        {
            if (r.State == ChargingRequestState.Cancelled) return;
            Ctx.Gateway.CancelChargingRequest(r.Id);
            r.CancelledByStudent = true;
        }
    }
}
