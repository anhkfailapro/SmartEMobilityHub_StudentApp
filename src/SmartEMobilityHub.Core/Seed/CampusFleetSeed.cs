using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Seed;

/// <summary>
/// Đội xe và dữ liệu riêng của kịch bản app Sinh viên, nạp vào CÙNG mạng lưới với Trung tâm điều hành (DC-54).
/// Trước đây app Sinh viên giữ các xe này trong MockDataStore riêng rồi đồng bộ sang Core; giờ chúng là xe thật
/// của mạng lưới nên Operator thấy, khoá, điều phối được ngay, không cần đồng bộ.
/// Không đổi bộ đếm chỗ đỗ của Hub (DC-25). Không nằm trong <see cref="NetworkSeed"/> để test Core giữ nguyên.
/// </summary>
public static class CampusFleetSeed
{
    /// <summary>Sinh viên khác đang giữ EB-1031 (UC-02: xe đã đặt chỉ xem, không đặt được).</summary>
    public const string OtherStudentId = "2310099";

    public static void Apply(NetworkStore store, DateTime now)
    {
        // Hub Ga Metro (HUB-01)
        Add(store, "EB-1024", VehicleType.EBike, "VinFast Evo200", "HUB-01", "A-07", 82, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "EM-2051", VehicleType.EMotorbike, "VinFast Feliz S", "HUB-01", "A-08", 58, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "EB-1031", VehicleType.EBike, "Giant Ease", "HUB-01", "A-09", 47, DeviceStatus.Available, now.AddMinutes(-3));
        Add(store, "EM-2060", VehicleType.EMotorbike, "Yadea Odora", "HUB-01", "A-10", 15, DeviceStatus.Faulty, now.AddMinutes(-14));
        Add(store, "EB-1040", VehicleType.EBike, "Pega eSmart", "HUB-01", "A-11", 91, DeviceStatus.Available, now.AddMinutes(-1));
        Add(store, "EM-2072", VehicleType.EMotorbike, "VinFast Vento", "HUB-01", "A-12", 64, DeviceStatus.Available, now.AddMinutes(-4));
        Add(store, "EB-1055", VehicleType.EBike, "Giant Ease", "HUB-01", "A-14", 23, DeviceStatus.Available, now.AddMinutes(-5));
        // Hub Thư viện Trung tâm (HUB-04)
        Add(store, "EB-2001", VehicleType.EBike, "VinFast Evo200", "HUB-04", "E-11", 76, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "EM-2101", VehicleType.EMotorbike, "Yadea Orla", "HUB-04", "E-12", 88, DeviceStatus.Available, now.AddMinutes(-3));
        Add(store, "EB-2010", VehicleType.EBike, "Pega eSmart", "HUB-04", "E-13", 54, DeviceStatus.Available, now.AddMinutes(-6));
        Add(store, "EM-2110", VehicleType.EMotorbike, "VinFast Feliz S", "HUB-04", "E-14", 18, DeviceStatus.NeedsCharge, now.AddMinutes(-2));
        // Hub KTX Khu A (HUB-02)
        Add(store, "EB-3001", VehicleType.EBike, "Giant Ease", "HUB-02", "C-11", 70, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "EM-3101", VehicleType.EMotorbike, "Yadea Orla", "HUB-02", "C-12", 45, DeviceStatus.Available, now.AddMinutes(-7));
        // Hub Nhà văn hóa SV (HUB-11)
        Add(store, "EM-4001", VehicleType.EMotorbike, "VinFast Vento", "HUB-11", "L-11", 30, DeviceStatus.Faulty, now.AddMinutes(-3));

        // EB-1031 đang được một sinh viên khác giữ (giữ đến hết 15 phút như mọi đặt chỗ khác).
        store.Reservations.Add(new Reservation
        {
            Id = "VR-KHAC-1",
            UserId = OtherStudentId,
            HubId = "HUB-01",
            VehicleCode = "EB-1031",
            Status = ReservationStatus.PendingPickup,
            CreatedAt = now.AddMinutes(-3),
            HoldUntil = now.AddMinutes(12),
            StartsAt = now.AddMinutes(12)
        });
        store.FindVehicle("EB-1031")!.Status = DeviceStatus.Reserved;

        // Xe sẵn sàng cuối cùng ở Hub Nhà văn hóa SV cũng đang được giữ: Hub này hết xe để đặt (UC-01 7a).
        store.Reservations.Add(new Reservation
        {
            Id = "VR-KHAC-2",
            UserId = OtherStudentId,
            HubId = "HUB-11",
            VehicleCode = "XE-136",
            Status = ReservationStatus.PendingPickup,
            CreatedAt = now.AddMinutes(-1),
            HoldUntil = now.AddMinutes(14),
            StartsAt = now.AddMinutes(14)
        });
        if (store.FindVehicle("XE-136") is { } xe136) xe136.Status = DeviceStatus.Reserved;

        // Báo lỗi cũ của sinh viên demo: báo lỗi lại EB-1031 sẽ được gộp vào sự cố này (UC-10).
        var incident = new IncidentRecord
        {
            IncidentId = store.NextIncidentId(),
            ObjectType = IncidentObjectType.Vehicle,
            ObjectCode = "EB-1031",
            HubId = "HUB-01",
            Source = IncidentSource.StudentReport,
            ProblemType = "Khóa không mở",
            Description = "Khóa xe không mở sau khi quét mã.",
            CreatedAt = now.AddDays(-1),
            Status = IncidentStatus.UnderCheck,
            Priority = PriorityLevel.Medium
        };
        incident.ReporterIds.Add(AccountSeed.StudentUsername);
        incident.Logs.Add(new IncidentLogEntry { At = now.AddDays(-1), Message = "Ghi nhận báo cáo: Khóa không mở.", Actor = "Sinh viên" });
        incident.Logs.Add(new IncidentLogEntry { At = now.AddDays(-1).AddMinutes(20), Message = "Chuyển trạng thái “Đang kiểm tra”.", Actor = "Người vận hành" });
        store.Incidents.Add(incident);
    }

    private static void Add(NetworkStore store, string code, VehicleType type, string model, string hubId, string slot,
        int battery, DeviceStatus status, DateTime updatedAt)
    {
        if (store.FindVehicle(code) is not null) return;
        store.GetHub(hubId);
        store.Vehicles.Add(new Vehicle
        {
            Code = code,
            Type = type,
            Model = model,
            HubId = hubId,
            Slot = slot,
            BatteryLevel = battery,
            Status = status,
            Connection = ConnectionStatus.Online,
            LastUpdatedAt = updatedAt
        });
    }
}
