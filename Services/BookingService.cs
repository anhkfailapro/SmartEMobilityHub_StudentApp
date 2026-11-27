using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class BookingService
    {
        private readonly StudentContext Ctx;

        public BookingService(StudentContext ctx) => Ctx = ctx;

        private MockDataStore Store => Ctx.Store;
        private DateTime Now => Ctx.Clock.Now;

        public PrivateEVChargingSession? ActiveSession =>
            Store.ChargingSessions.FirstOrDefault(s => s.State is SessionState.Pending or SessionState.Charging);

        public string? ValidateDuration(string hubId, int durationMinutes)
        {
            var hub = Store.GetHub(hubId);
            return durationMinutes > hub.MaxChargingMinutes
                ? $"{hub.Name} cho phép sạc tối đa {hub.MaxChargingMinutes} phút mỗi lần. Vui lòng chọn thời lượng ngắn hơn."
                : null;
        }

        public OpResult<PrivateEVChargingSession> ScheduleCharging(string hubId, string vehicleId, DateTime start, int durationMinutes, int targetBattery, bool alternative = false)
        {
            var hub = Store.GetHub(hubId);
            var durationError = ValidateDuration(hubId, durationMinutes);
            if (durationError != null) return OpResult<PrivateEVChargingSession>.Fail("DurationTooLong", durationError);
            var active = ActiveSession;
            if (active != null)
                return OpResult<PrivateEVChargingSession>.Fail("LimitReached", $"Bạn đã có 1 lịch sạc đang hoạt động ({DemoConfig.Time(active.Start)} tại {Store.GetHub(active.HubId).ShortName}). Hãy hủy hoặc hoàn tất lịch đó trước.");
            if (start < Now.AddMinutes(-1))
                return OpResult<PrivateEVChargingSession>.Fail("InPast", "Giờ bắt đầu đã qua. Vui lòng chọn giờ khác.");
            if (hub.IsSuspended)
                return OpResult<PrivateEVChargingSession>.Fail("NoPoint", $"{hub.Name} đang tạm dừng hoạt động theo quyết định của người vận hành.");
            var usable = hub.Points.Where(p => p.Status != PointStatus.OutOfService).ToList();
            if ((!alternative && Ctx.Toggles.NoChargingPointInWindow) || usable.Count == 0 || hub.FreeSlots <= 0)
                return OpResult<PrivateEVChargingSession>.Fail("NoPoint", $"Không còn cổng sạc hoặc chỗ đỗ trống tại {hub.Name} trong khung {DemoConfig.Time(start)}–{DemoConfig.Time(start.AddMinutes(durationMinutes))}.");

            var s = new PrivateEVChargingSession
            {
                Id = Store.NextId("CS"),
                PrivateVehicleId = vehicleId,
                HubId = hubId,
                PointNumber = usable[0].Number,
                SlotNumber = $"P-{Random.Shared.Next(1, 10):00}",
                Start = start,
                DurationMinutes = durationMinutes,
                TargetBattery = targetBattery,
                State = SessionState.Pending
            };
            Store.ChargingSessions.Add(s);
            Ctx.Notifications.Send("Đặt lịch sạc thành công", $"Cổng #{s.PointNumber} · Chỗ đỗ {s.SlotNumber} tại {hub.ShortName}, {DemoConfig.Time(s.Start)}–{DemoConfig.Time(s.End)} ngày {s.Start:dd/MM}. Nhắc trước {DemoConfig.ReminderMinutesBefore} phút.", Tone.Charging);
            return OpResult<PrivateEVChargingSession>.Success(s);
        }

        public DateTime FindEarliestTime(string hubId)
        {
            var n = Now.AddMinutes(10);
            return n.Date.AddHours(n.Hour).AddMinutes((n.Minute / 10 + 1) * 10);
        }

        public List<(Hub Hub, DateTime Start)> FindAlternativeWindows(string hubId, DateTime start)
        {
            var hub = Store.GetHub(hubId);
            var list = new List<(Hub Hub, DateTime Start)> { (hub, start.AddHours(1)), (hub, start.AddHours(2)) };
            foreach (var h in Store.Hubs.Where(h => h.Id != hubId && !h.IsSuspended && h.FreeSlots > 0 && h.Points.Any(p => p.Status != PointStatus.OutOfService)).OrderBy(h => h.DistanceKmTo(hub)).Take(2))
                list.Add((h, start));
            return list;
        }

        public int EstimateCost(int durationMinutes) => (int)Math.Round(durationMinutes / 60.0 * 1.2 * DemoConfig.PrivateChargingTariffPerKWh / 100) * 100;

        public void ProcessSessions()
        {
            foreach (var s in Store.ChargingSessions)
            {
                if (s.State == SessionState.Pending && !s.Reminded && Now >= s.Start.AddMinutes(-DemoConfig.ReminderMinutesBefore))
                {
                    s.Reminded = true;
                    Ctx.Notifications.Send("Nhắc lịch sạc", $"Lịch sạc của {Store.GetPrivateVehicle(s.PrivateVehicleId).Plate} bắt đầu lúc {DemoConfig.Time(s.Start)} tại {Store.GetHub(s.HubId).ShortName}, cổng #{s.PointNumber}.", Tone.Charging);
                }
                if (s.State == SessionState.Pending && Now >= s.Start) s.State = SessionState.Charging;
                if (s.State == SessionState.Charging && Now >= s.End)
                {
                    s.State = SessionState.Completed;
                    s.ClosedAt = s.End;
                }
            }
        }

        public string? ChargingPointOutOfService(bool otherPointAvailable)
        {
            var s = Store.ChargingSessions.FirstOrDefault(x => x.State == SessionState.Pending);
            if (s == null) return null;
            var hub = Store.GetHub(s.HubId);
            var old = s.PointNumber;
            var oldPoint = hub.Points.FirstOrDefault(p => p.Number == old);
            // Cổng hỏng thật trên mạng lưới: Operator nhận cảnh báo, xe đang sạc ở cổng đó được chuyển cổng (UC-SYS-01 5a).
            if (oldPoint != null && oldPoint.Status != PointStatus.OutOfService) Ctx.Gateway.MarkChargingPointFaulty(oldPoint.Id);
            if (otherPointAvailable)
            {
                var other = hub.Points.FirstOrDefault(p => p.Number != old && p.Status != PointStatus.OutOfService);
                if (other != null)
                {
                    s.PointNumber = other.Number;
                    Ctx.Notifications.Send("Lịch sạc đã được chuyển", $"Cổng #{old} tại {hub.ShortName} ngưng hoạt động. Lịch của bạn đã chuyển sang cổng #{other.Number}, giữ nguyên thời gian.", Tone.Warning);
                    return "Moved";
                }
            }
            s.State = SessionState.Cancelled;
            s.ClosedAt = Now;
            Ctx.Notifications.Send("Lịch sạc đã bị hủy", $"Cổng #{old} tại {hub.ShortName} ngưng hoạt động và không còn cổng trống. Lịch sạc {DemoConfig.Time(s.Start)} đã bị hủy, không mất phí.", Tone.Danger);
            return "Cancelled";
        }
    }
}
