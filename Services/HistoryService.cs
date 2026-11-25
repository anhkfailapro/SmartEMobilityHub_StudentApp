using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class HistoryService
    {
        private static MockDataStore Store => DemoApp.Store;

        public List<HistoryEntry> Entries()
        {
            var list = new List<HistoryEntry>();
            if (DemoApp.Toggles.HistoryEmpty) return list;
            var from = DemoApp.Clock.Now.AddMonths(-DemoConfig.HistoryMonths);

            foreach (var t in Store.Trips.Where(t => t.EndTime.HasValue && t.EndTime.Value >= from))
            {
                var start = Store.GetHub(t.StartHubId).ShortName;
                var end = t.EndHubId != null ? Store.GetHub(t.EndHubId).ShortName : "-";
                list.Add(new HistoryEntry
                {
                    Date = t.EndTime!.Value,
                    Title = $"Chuyến đi · {t.VehicleCode}",
                    HubName = $"{start} → {end}",
                    Duration = $"{t.Invoice?.Minutes ?? 0} phút",
                    Fee = DemoConfig.Money(t.Invoice?.Total ?? 0),
                    Status = "Đã thanh toán",
                    Trip = t
                });
            }
            foreach (var r in Store.VehicleReservations.Where(r => r.State is ReservationState.Cancelled or ReservationState.Expired && r.ClosedAt >= from))
            {
                list.Add(new HistoryEntry
                {
                    Date = r.ClosedAt!.Value,
                    Title = $"Đặt xe · {r.VehicleCode}",
                    HubName = Store.GetHub(r.HubId).ShortName,
                    Duration = $"{DemoConfig.HoldMinutes} phút giữ",
                    Fee = DemoConfig.Money(0),
                    Status = DemoConfig.ReservationStateLabel(r.State, true).Label
                });
            }
            foreach (var p in Store.ParkingReservations.Where(p => p.State != ReservationState.Active && p.ClosedAt >= from))
            {
                list.Add(new HistoryEntry
                {
                    Date = p.ClosedAt!.Value,
                    Title = $"Đỗ xe · {Store.GetPrivateVehicle(p.PrivateVehicleId).Plate}",
                    HubName = Store.GetHub(p.HubId).ShortName,
                    Duration = $"{p.DurationMinutes} phút",
                    Fee = DemoConfig.Money(p.CancellationFee),
                    Status = DemoConfig.ReservationStateLabel(p.State, false).Label
                });
            }
            foreach (var s in Store.ChargingSessions.Where(s => s.State is SessionState.Completed or SessionState.Cancelled or SessionState.Failed && s.ClosedAt >= from))
            {
                list.Add(new HistoryEntry
                {
                    Date = s.ClosedAt!.Value,
                    Title = $"Lịch sạc · {Store.GetPrivateVehicle(s.PrivateVehicleId).Plate}",
                    HubName = Store.GetHub(s.HubId).ShortName,
                    Duration = $"{s.DurationMinutes} phút",
                    // Phiên sạc hoàn tất phải hiện chi phí sạc; trước đây luôn hiện phí huỷ (0đ).
                    Fee = DemoConfig.Money(s.State == SessionState.Completed ? DemoApp.Booking.EstimateCost(s.DurationMinutes) : s.CancellationFee),
                    Status = DemoConfig.SessionStateLabel(s.State).Label
                });
            }
            return list.OrderByDescending(e => e.Date).ToList();
        }
    }
}
