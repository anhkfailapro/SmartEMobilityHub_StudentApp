using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class ChargingService
    {
        private static MockDataStore Store => DemoApp.Store;

        public ChargingEstimate GetChargingEstimate(string vehicleCode, string hubId, bool applyBusyToggle = true)
        {
            var hub = Store.GetHub(hubId);
            var v = Store.GetVehicle(vehicleCode);
            var allBusy = (applyBusyToggle && DemoApp.Toggles.AllChargingPointsBusy) || hub.FreePoints == 0;
            var position = hub.QueueLength + 1;
            var wait = allBusy ? 15 + position * 5 : position * 4;
            var battery = v?.Battery ?? 50;
            var urgency = Store.ActiveTrip != null ? 1.0 : 0.5;
            var score = Math.Round(100 * (0.5 * (1 - battery / 100.0) + 0.35 * urgency + 0.15 * Math.Min(1, wait / 30.0)));
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

        public List<ChargingEstimate> OtherHubs(string vehicleCode, string hubId) =>
            Store.Hubs.Where(h => h.Id != hubId).Select(h => GetChargingEstimate(vehicleCode, h.Id, false)).OrderBy(e => e.WaitMinutes).ToList();

        public ChargingRequest? OpenRequestFor(string vehicleCode) =>
            Store.ChargingRequests.FirstOrDefault(r => r.VehicleCode == vehicleCode && r.State != ChargingRequestState.Cancelled);

        public OpResult<ChargingRequest> SubmitChargingRequest(string vehicleCode, string hubId, bool applyBusyToggle = true)
        {
            var open = OpenRequestFor(vehicleCode);
            if (open != null)
                return OpResult<ChargingRequest>.Fail("Duplicate", $"{vehicleCode} đã có yêu cầu sạc đang mở tại {Store.GetHub(open.HubId).ShortName} (#{open.QueuePosition} trong hàng chờ). Không tạo yêu cầu mới.", open);
            var hub = Store.GetHub(hubId);
            if (hub.IsSuspended)
                return OpResult<ChargingRequest>.Fail("HubSuspended", $"{hub.Name} đang tạm dừng hoạt động. Vui lòng chọn Hub khác.");
            var e = GetChargingEstimate(vehicleCode, hubId, applyBusyToggle);
            var r = new ChargingRequest
            {
                Id = Store.NextId("CR"),
                VehicleCode = vehicleCode,
                HubId = hubId,
                Score = e.Score,
                QueuePosition = e.QueuePosition,
                WaitMinutes = e.WaitMinutes,
                State = ChargingRequestState.InQueue,
                CreatedAt = DemoApp.Clock.Now
            };
            e.Hub.QueueLength++;
            Store.ChargingRequests.Add(r);
            DemoApp.Link.OnChargingRequested(r);
            DemoApp.Notifications.Send("Đã gửi yêu cầu sạc", $"{vehicleCode} ở vị trí #{r.QueuePosition} tại {e.Hub.ShortName}, chờ khoảng {r.WaitMinutes} phút.", Tone.Charging);
            return OpResult<ChargingRequest>.Success(r);
        }

        /// <summary>
        /// Demo panel "Cấp cổng sạc": cấp cổng trống cho yêu cầu đầu hàng chờ có cổng trống.
        /// Trước đây khi Hub hết cổng trống, hàm lấy luôn cổng đầu tiên (kể cả cổng đang sạc xe khác
        /// hoặc cổng hỏng) nên một cổng bị cấp cho hai xe.
        /// </summary>
        public ChargingRequest? AssignNextChargingPoint()
        {
            foreach (var r in Store.ChargingRequests.Where(x => x.State == ChargingRequestState.InQueue).ToList())
            {
                var point = Store.GetHub(r.HubId).Points.FirstOrDefault(p => p.Status == PointStatus.Free);
                if (point != null) return AssignChargingPoint(r, point.Number);
            }
            return null;
        }

        /// <summary>Cấp cổng số <paramref name="pointNumber"/> cho yêu cầu (gọi từ Demo panel hoặc khi lập lịch của Core cấp cổng).</summary>
        public ChargingRequest? AssignChargingPoint(ChargingRequest r, int pointNumber)
        {
            if (r.State != ChargingRequestState.InQueue) return null;
            var hub = Store.GetHub(r.HubId);
            var point = hub.Points.FirstOrDefault(p => p.Number == pointNumber);
            if (point == null || point.Status == PointStatus.OutOfService) return null;
            point.Status = PointStatus.Charging;
            r.State = ChargingRequestState.Assigned;
            r.AssignedPoint = point.Number;
            hub.QueueLength = Math.Max(0, hub.QueueLength - 1);
            var v = Store.GetVehicle(r.VehicleCode);
            if (v != null && v.Status != VehicleStatus.InUse) v.Status = VehicleStatus.Charging;
            DemoApp.Notifications.Send("Đã có cổng sạc", $"Cổng sạc #{point.Number} tại {hub.ShortName} đã sẵn sàng cho {r.VehicleCode}.", Tone.Charging);
            return r;
        }

        /// <summary>
        /// Trả tài nguyên của một yêu cầu sạc sắp bị huỷ: đang chờ thì rời hàng chờ, đã có cổng thì trả cổng.
        /// Trước đây huỷ yêu cầu đã có cổng vẫn trừ độ dài hàng chờ và cổng bị giữ ở trạng thái đang sạc mãi.
        /// </summary>
        public void Release(ChargingRequest r)
        {
            var hub = Store.GetHub(r.HubId);
            if (r.State == ChargingRequestState.InQueue)
            {
                hub.QueueLength = Math.Max(0, hub.QueueLength - 1);
                return;
            }
            if (r.State != ChargingRequestState.Assigned) return;

            var point = hub.Points.FirstOrDefault(p => p.Number == r.AssignedPoint);
            if (point != null && point.Status == PointStatus.Charging) point.Status = PointStatus.Free;
            var v = Store.GetVehicle(r.VehicleCode);
            if (v != null && v.Status == VehicleStatus.Charging) v.Status = v.HubId == null ? VehicleStatus.InUse : VehicleStatus.Available;
        }
    }
}
