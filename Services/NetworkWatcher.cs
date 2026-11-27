using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    /// <summary>
    /// Báo cho sinh viên những thay đổi do Trung tâm điều hành (hoặc chu kỳ quét) tạo ra trên mạng lưới chung (DC-54):
    /// Hub tạm dừng / mở lại, đặt xe bị huỷ hoặc hết hạn, yêu cầu sạc được cấp cổng, sự cố đã xử lý xong.
    /// Không chép dữ liệu (app đọc thẳng mạng lưới) — chỉ so trạng thái giữa hai nhịp để gửi thông báo và
    /// biết khi nào cần vẽ lại màn hình. Lần quan sát đầu tiên chỉ ghi mốc, không thông báo.
    /// </summary>
    public sealed class NetworkWatcher
    {
        private readonly StudentContext Ctx;
        private readonly Dictionary<string, bool> _hubSuspended = new();
        private readonly Dictionary<string, ReservationState> _reservations = new();
        private readonly Dictionary<string, ChargingRequestState> _charging = new();
        private readonly Dictionary<string, IncidentStatus> _incidents = new();
        private string _fingerprint = "";

        public NetworkWatcher(StudentContext ctx) => Ctx = ctx;

        /// <summary>Quên mọi mốc (sau khi đặt lại demo / tạo mạng lưới mới).</summary>
        public void Reset()
        {
            _hubSuspended.Clear();
            _reservations.Clear();
            _charging.Clear();
            _incidents.Clear();
            _fingerprint = "";
        }

        /// <summary>Một nhịp (mỗi giây). Trả true nếu dữ liệu sinh viên đang xem đã đổi (cần vẽ lại màn hình).</summary>
        public bool Poll()
        {
            var store = Ctx.Store;
            var notify = Ctx.Notifications;

            foreach (var hub in store.Hubs)
            {
                var suspended = hub.IsSuspended;
                if (_hubSuspended.TryGetValue(hub.Id, out var before) && before != suspended)
                {
                    notify.Send(
                        suspended ? "Hub tạm dừng hoạt động" : "Hub hoạt động trở lại",
                        suspended
                            ? $"{hub.Name} tạm dừng theo quyết định của người vận hành. Không thể đặt xe, đỗ xe hoặc sạc tại đây."
                            : $"{hub.Name} đã mở lại, bạn có thể đặt xe và đỗ xe như bình thường.",
                        suspended ? Tone.Danger : Tone.Success);
                }
                _hubSuspended[hub.Id] = suspended;
            }

            foreach (var r in store.VehicleReservations)
            {
                var state = r.State;
                if (_reservations.TryGetValue(r.Id, out var before) && before == ReservationState.Active && state != before)
                {
                    if (state == ReservationState.Expired)
                        notify.Send("Đặt chỗ đã bị hủy", $"Hết thời gian giữ xe {r.VehicleCode}. Xe đã trở về trạng thái sẵn sàng.", Tone.Danger);
                    else if (state == ReservationState.Cancelled && IsSystemCancel(r.CancelReason))
                        notify.Send("Đặt xe đã bị hủy", $"Đặt xe {r.VehicleCode} đã bị hệ thống hủy: {r.CancelReason}. Không mất phí.", Tone.Danger);
                }
                _reservations[r.Id] = state;
            }

            foreach (var q in store.ChargingRequests)
            {
                var state = q.State;
                if (_charging.TryGetValue(q.Id, out var before) && before == ChargingRequestState.InQueue && state == ChargingRequestState.Assigned)
                    notify.Send("Đã có cổng sạc", $"Cổng sạc #{q.AssignedPoint} tại {store.GetHub(q.HubId).ShortName} đã sẵn sàng cho {q.VehicleCode}.", Tone.Charging);
                else if (_charging.TryGetValue(q.Id, out before) && before != ChargingRequestState.Cancelled
                         && state == ChargingRequestState.Cancelled && !q.CancelledByStudent)
                    notify.Send("Yêu cầu sạc đã kết thúc", $"Yêu cầu sạc {q.VehicleCode} tại {store.GetHub(q.HubId).ShortName} đã được hệ thống đóng.", Tone.Info);
                _charging[q.Id] = state;
            }

            foreach (var i in store.Incidents)
            {
                var status = i.Status;
                if (_incidents.TryGetValue(i.Code, out var before) && before != status
                    && status is IncidentStatus.Resolved or IncidentStatus.Closed
                    && before is not (IncidentStatus.Resolved or IncidentStatus.Closed))
                {
                    notify.Send("Sự cố đã được xử lý",
                        $"Báo cáo {i.Code} ({i.ObjectCode}) đã được người vận hành xử lý xong. Cảm ơn bạn đã báo lỗi.", Tone.Success);
                }
                _incidents[i.Code] = status;
            }

            var fingerprint = Fingerprint();
            var changed = _fingerprint != "" && fingerprint != _fingerprint;
            _fingerprint = fingerprint;
            return changed;
        }

        /// <summary>Huỷ do sinh viên tự huỷ hoặc do lỗi mở khoá (màn hình đã báo) thì không gửi thêm thông báo.</summary>
        private static bool IsSystemCancel(string? reason) =>
            reason is not null
            && reason != StudentAppGateway.CancelledByStudent
            && !reason.StartsWith("Không xác nhận mở khoá", StringComparison.Ordinal);

        /// <summary>Tóm tắt những gì app Sinh viên hiển thị; khác đi nghĩa là cần vẽ lại.</summary>
        private string Fingerprint()
        {
            var network = Ctx.Network.Store;
            var parts = new List<string>();
            foreach (var h in network.Hubs)
                parts.Add($"{h.Id}:{h.Status}:{h.Connection}:{h.OccupiedParkingSlots}:{string.Concat(h.ChargingPoints.Select(p => (int)p.Status))}");
            foreach (var v in network.Vehicles)
                parts.Add($"{v.Code}:{v.Status}:{v.HubId}:{v.BatteryLevel}:{v.Connection}");
            foreach (var q in network.ChargingRequests)
                parts.Add($"{q.RequestId}:{q.AssignedPortId}:{q.QueuePosition}");
            foreach (var r in Ctx.Store.VehicleReservations) parts.Add($"{r.Id}:{r.State}");
            foreach (var i in Ctx.Store.Incidents) parts.Add($"{i.Code}:{i.Status}");
            return string.Join("|", parts);
        }
    }
}
