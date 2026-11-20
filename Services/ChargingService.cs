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
            DemoApp.Notifications.Send("Đã gửi yêu cầu sạc", $"{vehicleCode} ở vị trí #{r.QueuePosition} tại {e.Hub.ShortName}, chờ khoảng {r.WaitMinutes} phút.", Tone.Charging);
            return OpResult<ChargingRequest>.Success(r);
        }

        public ChargingRequest? AssignNextChargingPoint()
        {
            var r = Store.ChargingRequests.FirstOrDefault(x => x.State == ChargingRequestState.InQueue);
            if (r == null) return null;
            var hub = Store.GetHub(r.HubId);
            var point = hub.Points.FirstOrDefault(p => p.Status == PointStatus.Free) ?? hub.Points.First();
            point.Status = PointStatus.Charging;
            r.State = ChargingRequestState.Assigned;
            r.AssignedPoint = point.Number;
            hub.QueueLength = Math.Max(0, hub.QueueLength - 1);
            var v = Store.GetVehicle(r.VehicleCode);
            if (v != null && v.Status != VehicleStatus.InUse) v.Status = VehicleStatus.Charging;
            DemoApp.Notifications.Send("Đã có cổng sạc", $"Cổng sạc #{point.Number} tại {hub.ShortName} đã sẵn sàng cho {r.VehicleCode}.", Tone.Charging);
            return r;
        }
    }
}
