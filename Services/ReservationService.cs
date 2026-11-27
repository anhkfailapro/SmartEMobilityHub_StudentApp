using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Services
{
    public class ReservationService
    {
        private readonly StudentContext Ctx;

        public ReservationService(StudentContext ctx) => Ctx = ctx;

        private MockDataStore Store => Ctx.Store;
        private DateTime Now => Ctx.Clock.Now;

        public VehicleReservation? ActiveVehicleReservation => Store.VehicleReservations.FirstOrDefault(r => r.State == ReservationState.Active);

        public string CheckReserve(string vehicleCode)
        {
            if (Store.ActiveTrip != null) return "TripInProgress";
            if (Store.VehicleReservations.Count(r => r.State == ReservationState.Active) >= DemoConfig.MaxActiveVehicle) return "AlreadyActive";
            var v = Store.GetVehicle(vehicleCode);
            if (v == null || !v.IsAvailable) return "TakenByOther";
            if (v.HubId != null && Store.GetHub(v.HubId).IsSuspended) return "HubSuspended";
            if (Ctx.Toggles.AnotherStudentReservesFirst)
            {
                // UC-03 2a: một sinh viên khác đặt trước — ghi đặt chỗ thật của người đó vào mạng lưới.
                Ctx.Toggles.AnotherStudentReservesFirst = false;
                try
                {
                    Ctx.Gateway.ReserveVehicle(Store.NextId("VR-KHAC"), CampusFleetSeed.OtherStudentId, v.Code, v.HubId ?? "",
                        Now.AddMinutes(DemoConfig.HoldMinutes));
                }
                catch (Exception ex) when (ex is CoreDomain.IllegalStateException or CoreDomain.IllegalArgumentException)
                {
                    // Xe đã không còn đặt được thì kết quả vẫn là "đã có người đặt".
                }
                return "TakenByOther";
            }
            return "";
        }

        public OpResult<VehicleReservation> ReserveVehicle(string vehicleCode)
        {
            var error = CheckReserve(vehicleCode);
            if (error != "") return OpResult<VehicleReservation>.Fail(error);
            var v = Store.GetVehicle(vehicleCode)!;
            CoreDomain.Reservation core;
            try
            {
                core = Ctx.Gateway.ReserveVehicle(Store.NextId("VR"), Store.StudentId, v.Code, v.HubId ?? "",
                    Now.AddMinutes(DemoConfig.HoldMinutes));
            }
            catch (CoreDomain.IllegalStateException ex)
            {
                // Mạng lưới từ chối (Hub vừa bị tạm dừng, xe vừa đổi trạng thái): không giữ xe trên app.
                return OpResult<VehicleReservation>.Fail(Store.GetHub(v.HubId ?? "").IsSuspended ? "HubSuspended" : "TakenByOther", ex.Message);
            }
            var r = Store.Track(core, v.Slot, GeneratePickUpCode());
            Ctx.Notifications.Send("Đặt xe thành công", $"Xe {v.Code} được giữ đến {DemoConfig.Time(r.HoldUntil)} tại {Store.GetHub(r.HubId).ShortName}. Mã nhận xe {r.PickUpCode}.", Tone.Success);
            return OpResult<VehicleReservation>.Success(r);
        }

        public static string GeneratePickUpCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var a = new string(Enumerable.Range(0, 3).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
            return $"{a}-{Random.Shared.Next(10, 100)}";
        }

        /// <summary>
        /// Huỷ đặt xe quá 15 phút của sinh viên, dùng đúng quy tắc của Core (UC-SYS-04, gia hạn có giới hạn khi
        /// đang quét QR). Trả về các đặt xe vừa hết hạn; thông báo do NetworkWatcher gửi.
        /// </summary>
        public List<VehicleReservation> AutoCancelExpired()
        {
            var active = Store.VehicleReservations.Where(r => r.State == ReservationState.Active).ToList();
            if (active.Count == 0) return new List<VehicleReservation>();
            Ctx.Gateway.AutoCancelExpiredFor(Store.StudentId);
            return active.Where(r => r.State == ReservationState.Expired).ToList();
        }

        public List<BookingItem> ListActive()
        {
            var items = new List<BookingItem>();
            foreach (var r in Store.VehicleReservations.Where(r => r.State is ReservationState.Active or ReservationState.Started))
            {
                var hub = Store.GetHub(r.HubId);
                var started = r.State == ReservationState.Started;
                var it = new BookingItem { Kind = BookingKind.Vehicle, Id = r.Id, Title = $"Đặt xe · {r.VehicleCode}", HubName = hub.ShortName, StatusLabel = started ? "Đã nhận xe" : "Đang giữ", StatusTone = started ? Tone.Info : Tone.Warning, Started = started, IsActive = true };
                it.Rows.Add(("Hub", hub.ShortName));
                if (!started)
                {
                    var left = Math.Max(0, (int)Math.Ceiling((r.HoldUntil - Now).TotalMinutes));
                    it.Rows.Add(("Giữ đến", $"{DemoConfig.Time(r.HoldUntil)} (còn {left} phút)"));
                    it.Rows.Add(("Mã nhận xe", r.PickUpCode));
                }
                else
                {
                    it.Rows.Add(("Trạng thái", "Chuyến đi đang diễn ra"));
                }
                items.Add(it);
            }
            foreach (var p in Store.ParkingReservations.Where(p => p.State == ReservationState.Active))
            {
                var hub = Store.GetHub(p.HubId);
                var pv = Store.GetPrivateVehicle(p.PrivateVehicleId);
                var it = new BookingItem { Kind = BookingKind.Parking, Id = p.Id, Title = $"Đỗ xe · {pv.Plate}", HubName = hub.ShortName, StatusLabel = "Đã xác nhận", StatusTone = Tone.Info, Start = p.Start, CanChangeTime = true, IsActive = true };
                it.Rows.Add(("Hub", hub.ShortName));
                it.Rows.Add(("Chỗ đỗ", p.SlotNumber));
                it.Rows.Add(("Thời gian", $"{p.Start:dd/MM} · {DemoConfig.Time(p.Start)}–{DemoConfig.Time(p.End)}"));
                items.Add(it);
            }
            foreach (var s in Store.ChargingSessions.Where(s => s.State is SessionState.Pending or SessionState.Charging))
            {
                var hub = Store.GetHub(s.HubId);
                var pv = Store.GetPrivateVehicle(s.PrivateVehicleId);
                var (label, tone) = DemoConfig.SessionStateLabel(s.State);
                var it = new BookingItem { Kind = BookingKind.ChargingSession, Id = s.Id, Title = $"Lịch sạc · {pv.Plate}", HubName = hub.ShortName, StatusLabel = label, StatusTone = tone == Tone.Neutral ? Tone.Charging : tone, Start = s.Start, Started = s.State == SessionState.Charging, CanChangeTime = s.State == SessionState.Pending, IsActive = true };
                it.Rows.Add(("Hub", hub.ShortName));
                it.Rows.Add(("Cổng sạc", $"#{s.PointNumber} · Chỗ đỗ {s.SlotNumber}"));
                it.Rows.Add(("Thời gian", $"{s.Start:dd/MM} · {DemoConfig.Time(s.Start)}–{DemoConfig.Time(s.End)}"));
                it.Rows.Add(("Pin mục tiêu", $"{s.TargetBattery}%"));
                items.Add(it);
            }
            foreach (var q in Store.ChargingRequests.Where(q => q.State is ChargingRequestState.InQueue or ChargingRequestState.Assigned))
            {
                var hub = Store.GetHub(q.HubId);
                var assigned = q.State == ChargingRequestState.Assigned;
                var it = new BookingItem { Kind = BookingKind.ChargingRequest, Id = q.Id, Title = $"Yêu cầu sạc · {q.VehicleCode}", HubName = hub.ShortName, StatusLabel = assigned ? "Đã có cổng sạc" : "Trong hàng chờ", StatusTone = assigned ? Tone.Success : Tone.Charging, IsActive = true };
                it.Rows.Add(("Hub", hub.ShortName));
                it.Rows.Add(assigned ? ("Cổng sạc", $"#{q.AssignedPoint}") : ("Vị trí hàng chờ", $"#{q.QueuePosition} · ~{q.WaitMinutes} phút"));
                it.Rows.Add(("Mức ưu tiên", DemoConfig.PriorityLabel(DemoConfig.PriorityFromScore(q.Score))));
                items.Add(it);
            }
            return items;
        }

        public List<BookingItem> ListFinished()
        {
            var items = new List<BookingItem>();
            foreach (var r in Store.VehicleReservations.Where(r => r.State is ReservationState.Cancelled or ReservationState.Expired or ReservationState.Completed))
            {
                var (label, tone) = DemoConfig.ReservationStateLabel(r.State, true);
                var it = new BookingItem { Kind = BookingKind.Vehicle, Id = r.Id, Title = $"Đặt xe · {r.VehicleCode}", HubName = Store.GetHub(r.HubId).ShortName, StatusLabel = label, StatusTone = tone };
                it.Rows.Add(("Hub", it.HubName));
                it.Rows.Add(("Kết thúc", r.ClosedAt.HasValue ? DemoConfig.DateTimeShort(r.ClosedAt.Value) : "-"));
                items.Add(it);
            }
            foreach (var p in Store.ParkingReservations.Where(p => p.State != ReservationState.Active))
            {
                var (label, tone) = DemoConfig.ReservationStateLabel(p.State, false);
                var it = new BookingItem { Kind = BookingKind.Parking, Id = p.Id, Title = $"Đỗ xe · {Store.GetPrivateVehicle(p.PrivateVehicleId).Plate}", HubName = Store.GetHub(p.HubId).ShortName, StatusLabel = label, StatusTone = tone };
                it.Rows.Add(("Hub", it.HubName));
                it.Rows.Add(("Thời gian", $"{p.Start:dd/MM} · {DemoConfig.Time(p.Start)}–{DemoConfig.Time(p.End)}"));
                if (p.CancellationFee > 0) it.Rows.Add(("Phí hủy muộn", DemoConfig.Money(p.CancellationFee)));
                items.Add(it);
            }
            foreach (var s in Store.ChargingSessions.Where(s => s.State is SessionState.Completed or SessionState.Failed or SessionState.Cancelled))
            {
                var (label, tone) = DemoConfig.SessionStateLabel(s.State);
                var it = new BookingItem { Kind = BookingKind.ChargingSession, Id = s.Id, Title = $"Lịch sạc · {Store.GetPrivateVehicle(s.PrivateVehicleId).Plate}", HubName = Store.GetHub(s.HubId).ShortName, StatusLabel = label, StatusTone = tone };
                it.Rows.Add(("Hub", it.HubName));
                it.Rows.Add(("Thời gian", $"{s.Start:dd/MM} · {DemoConfig.Time(s.Start)}–{DemoConfig.Time(s.End)}"));
                if (s.CancellationFee > 0) it.Rows.Add(("Phí hủy muộn", DemoConfig.Money(s.CancellationFee)));
                items.Add(it);
            }
            foreach (var q in Store.ChargingRequests.Where(q => q.State == ChargingRequestState.Cancelled))
            {
                var it = new BookingItem { Kind = BookingKind.ChargingRequest, Id = q.Id, Title = $"Yêu cầu sạc · {q.VehicleCode}", HubName = Store.GetHub(q.HubId).ShortName, StatusLabel = "Đã hủy", StatusTone = Tone.Neutral };
                it.Rows.Add(("Hub", it.HubName));
                items.Add(it);
            }
            return items;
        }

        public BookingItem? FindActive(BookingKind kind, string id) => ListActive().FirstOrDefault(i => i.Kind == kind && i.Id == id);

        public OpResult<int> PreviewCancel(BookingKind kind, string id)
        {
            switch (kind)
            {
                case BookingKind.Vehicle:
                    {
                        var r = Store.VehicleReservations.First(x => x.Id == id);
                        if (r.State == ReservationState.Started)
                            return OpResult<int>.Fail("Started", $"Bạn đã nhận xe {r.VehicleCode}, chuyến đi đã bắt đầu. Hãy trả xe để kết thúc chuyến đi.");
                        return OpResult<int>.Success(0);
                    }
                case BookingKind.Parking:
                    {
                        var p = Store.ParkingReservations.First(x => x.Id == id);
                        return OpResult<int>.Success(IsLate(p.Start) ? DemoConfig.LateCancellationFee : 0);
                    }
                case BookingKind.ChargingSession:
                    {
                        var s = Store.ChargingSessions.First(x => x.Id == id);
                        if (s.State == SessionState.Charging)
                            return OpResult<int>.Fail("Started", "Phiên sạc đã bắt đầu nên không thể hủy.");
                        return OpResult<int>.Success(IsLate(s.Start) ? DemoConfig.LateCancellationFee : 0);
                    }
                default:
                    return OpResult<int>.Success(0);
            }
        }

        public bool IsLate(DateTime start) => (start - Now).TotalMinutes < DemoConfig.LateCancelWindowMinutes;

        public int Cancel(BookingKind kind, string id)
        {
            var preview = PreviewCancel(kind, id);
            if (!preview.Ok) return -1;
            var fee = preview.Value;
            string what;
            switch (kind)
            {
                case BookingKind.Vehicle:
                    {
                        var r = Store.VehicleReservations.First(x => x.Id == id);
                        Ctx.Gateway.CloseReservation(r.Id, StudentAppGateway.CancelledByStudent);
                        what = $"đặt xe {r.VehicleCode}";
                        break;
                    }
                case BookingKind.Parking:
                    {
                        var p = Store.ParkingReservations.First(x => x.Id == id);
                        p.State = ReservationState.Cancelled;
                        p.CancellationFee = fee;
                        p.ClosedAt = Now;
                        Ctx.Gateway.ChangeParkingOccupancy(p.HubId, -1);
                        what = $"đặt chỗ đỗ {p.SlotNumber}";
                        break;
                    }
                case BookingKind.ChargingSession:
                    {
                        var s = Store.ChargingSessions.First(x => x.Id == id);
                        s.State = SessionState.Cancelled;
                        s.CancellationFee = fee;
                        s.ClosedAt = Now;
                        what = $"lịch sạc cổng #{s.PointNumber}";
                        break;
                    }
                default:
                    {
                        var q = Store.ChargingRequests.First(x => x.Id == id);
                        Ctx.Charging.Cancel(q);
                        what = $"yêu cầu sạc {q.VehicleCode}";
                        break;
                    }
            }
            var feeText = fee > 0 ? $" Phí hủy muộn: {DemoConfig.Money(fee)}." : " Không mất phí.";
            Ctx.Notifications.Send("Đã hủy đặt chỗ", $"Bạn đã hủy {what}.{feeText}", Tone.Info);
            return fee;
        }

        public OpResult<bool> ChangeTime(BookingKind kind, string id, DateTime newStart, int durationMinutes)
        {
            if (newStart < Now) return OpResult<bool>.Fail("InPast", "Giờ bắt đầu mới đã qua. Vui lòng chọn giờ khác.");
            if (kind == BookingKind.Parking)
            {
                var p = Store.ParkingReservations.First(x => x.Id == id);
                var old = $"{DemoConfig.Time(p.Start)}–{DemoConfig.Time(p.End)}";
                if (Ctx.Toggles.ChangeTimeUnavailable)
                    return OpResult<bool>.Fail("Unavailable", $"Khung giờ mới không còn chỗ trống. Lịch cũ {old} được giữ nguyên.");
                p.Start = newStart;
                p.DurationMinutes = durationMinutes;
                Ctx.Notifications.Send("Đã đổi giờ đặt chỗ", $"Chỗ đỗ {p.SlotNumber}: {DemoConfig.Time(p.Start)}–{DemoConfig.Time(p.End)} ngày {p.Start:dd/MM}.", Tone.Info);
                return OpResult<bool>.Success(true);
            }
            if (kind == BookingKind.ChargingSession)
            {
                var s = Store.ChargingSessions.First(x => x.Id == id);
                var hub = Store.GetHub(s.HubId);
                var old = $"{DemoConfig.Time(s.Start)}–{DemoConfig.Time(s.End)}";
                if (durationMinutes > hub.MaxChargingMinutes)
                    return OpResult<bool>.Fail("DurationTooLong", $"{hub.Name} cho phép sạc tối đa {hub.MaxChargingMinutes} phút. Lịch cũ {old} được giữ nguyên.");
                if (Ctx.Toggles.ChangeTimeUnavailable)
                    return OpResult<bool>.Fail("Unavailable", $"Không còn cổng sạc trống trong khung giờ mới. Lịch cũ {old} được giữ nguyên.");
                s.Start = newStart;
                s.DurationMinutes = durationMinutes;
                s.Reminded = false;
                Ctx.Notifications.Send("Đã đổi giờ lịch sạc", $"Cổng #{s.PointNumber} tại {hub.ShortName}: {DemoConfig.Time(s.Start)}–{DemoConfig.Time(s.End)} ngày {s.Start:dd/MM}.", Tone.Charging);
                return OpResult<bool>.Success(true);
            }
            return OpResult<bool>.Fail("NotSupported", "Đặt xe chia sẻ là giữ cố định 15 phút, không thể đổi giờ.");
        }
    }
}
