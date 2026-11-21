using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Seed;

/// <summary>
/// Reservation phục vụ TCSYS04-01..03 và TCOP03-04 (Bảng 65/67).
/// HoldUntil là nguồn sự thật của hạn giữ chỗ; ExtendHold sẽ đẩy mốc này ra.
/// </summary>
public static class ReservationSeed
{
    public static void Apply(NetworkStore store, DateTime now)
    {
        // R-101: tạo 16 phút trước, không nhận xe → phải bị huỷ.
        store.Reservations.Add(new Reservation
        {
            Id = "R-101",
            UserId = "SV-2414064",
            HubId = "HUB-01",
            VehicleCode = "XE-105",
            Status = ReservationStatus.PendingPickup,
            CreatedAt = now.AddMinutes(-16),
            HoldUntil = now.AddMinutes(-1),
            StartsAt = now.AddMinutes(-16)
        });

        // R-102: tạo 14 phút trước → còn hạn, không được huỷ (biên).
        store.Reservations.Add(new Reservation
        {
            Id = "R-102",
            UserId = "SV-2414065",
            HubId = "HUB-03",
            VehicleCode = "XE-111",
            Status = ReservationStatus.PendingPickup,
            CreatedAt = now.AddMinutes(-14),
            HoldUntil = now.AddMinutes(1),
            StartsAt = now.AddMinutes(-14)
        });

        // R-103: quá 15 phút nhưng đang quét QR → extendHold, không huỷ (Exception 5a).
        store.Reservations.Add(new Reservation
        {
            Id = "R-103",
            UserId = "SV-2414066",
            HubId = "HUB-07",
            VehicleCode = "XE-121",
            Status = ReservationStatus.PendingPickup,
            CreatedAt = now.AddMinutes(-16),
            HoldUntil = now.AddMinutes(-1),
            StartsAt = now.AddMinutes(-16),
            IsPickingUp = true
        });

        // 5 reservation sắp tới tại HUB-08 — suspendHub phải huỷ đúng 5 và trả về 5.
        var upcoming = new[]
        {
            ("R-104", "XE-139", 20),
            ("R-105", "XE-140", 35),
            ("R-106", "XE-141", 50),
            ("R-107", "XE-142", 75),
            ("R-108", "XE-143", 110)
        };

        var index = 0;
        foreach (var (id, vehicleCode, startInMinutes) in upcoming)
        {
            index++;
            store.Reservations.Add(new Reservation
            {
                Id = id,
                UserId = $"SV-24140{70 + index}",
                HubId = "HUB-08",
                VehicleCode = vehicleCode,
                Status = ReservationStatus.PendingPickup,
                CreatedAt = now.AddMinutes(-5),
                HoldUntil = now.AddMinutes(startInMinutes + ServiceConstants.ReservationHoldMinutes),
                StartsAt = now.AddMinutes(startInMinutes)
            });
        }
    }
}
