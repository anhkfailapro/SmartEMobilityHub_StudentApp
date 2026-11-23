using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Ports;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Adapters;

/// <summary>
/// Cài đặt mặc định của IReservationPort trên NetworkStore. Khi ReservationManager của
/// Trường sẵn sàng, chỉ cần viết adapter mới — AutoCancelService không phải sửa.
/// </summary>
public sealed class InMemoryReservationAdapter : IReservationPort
{
    private readonly NetworkStore _store;
    private readonly IClock _clock;

    public InMemoryReservationAdapter(NetworkStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public IReadOnlyList<Reservation> FindExpiredReservations(DateTime now, int holdMinutes)
    {
        return _store.Reservations
            .Where(r => r.Status == ReservationStatus.PendingPickup)
            .Where(r => EffectiveDeadline(r, holdMinutes) <= now)
            .ToList();
    }

    public IReadOnlyList<Reservation> FindUpcomingByHub(string hubId, DateTime now)
    {
        return _store.Reservations
            .Where(r => r.HubId == hubId && r.Status == ReservationStatus.PendingPickup && r.StartsAt > now)
            .OrderBy(r => r.StartsAt)
            .ToList();
    }

    public void CancelReservation(string reservationId, string reason)
    {
        var reservation = _store.Reservations.FirstOrDefault(r => r.Id == reservationId)
                          ?? throw new IllegalArgumentException($"Không tìm thấy đặt chỗ {reservationId}.");

        reservation.Status = ReservationStatus.Cancelled;
        reservation.ClosedAt = _clock.Now;
        reservation.CancelReason = reason;
        reservation.IsPickingUp = false;

        var vehicle = _store.FindVehicle(reservation.VehicleCode);
        if (vehicle is not null && vehicle.Status == DeviceStatus.Reserved)
            vehicle.Status = DeviceStatus.Available;
    }

    public void ExtendHold(string reservationId, int extraMinutes)
    {
        var reservation = _store.Reservations.FirstOrDefault(r => r.Id == reservationId)
                          ?? throw new IllegalArgumentException($"Không tìm thấy đặt chỗ {reservationId}.");

        reservation.HoldUntil = _clock.Now.AddMinutes(extraMinutes);
        reservation.ExtensionCount++;
    }

    public void MarkPickedUp(string reservationId)
    {
        var reservation = _store.Reservations.FirstOrDefault(r => r.Id == reservationId)
                          ?? throw new IllegalArgumentException($"Không tìm thấy đặt chỗ {reservationId}.");

        reservation.Status = ReservationStatus.PickedUp;
        reservation.IsPickingUp = false;

        var vehicle = _store.FindVehicle(reservation.VehicleCode);
        if (vehicle is not null)
        {
            vehicle.Status = DeviceStatus.InUse;
            vehicle.AssignedPortId = null;
        }
    }

    /// <summary>
    /// Hạn hiệu lực = muộn hơn giữa "tạo + 15 phút" và HoldUntil (sau khi được gia hạn).
    /// Nhờ vậy vừa đúng ngưỡng 15 phút, vừa tôn trọng extendHold.
    /// </summary>
    private static DateTime EffectiveDeadline(Reservation reservation, int holdMinutes)
    {
        var byCreated = reservation.CreatedAt.AddMinutes(holdMinutes);
        return reservation.HoldUntil > byCreated ? reservation.HoldUntil : byCreated;
    }
}
