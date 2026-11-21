using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Seed;

/// <summary>
/// 7 sự cố hero của Sub 2 §6.2 (SC-1011, 1014, 1017, 1019, 1022, 1023, 1024)
/// + 17 sự cố sinh có seed cố định, tổng đúng 24 theo phân bố Figma:
/// Mới 12 (8 sinh viên + 4 IoT), Kiểm tra 7 (5 xe + 2 cổng), Sửa chữa 5.
/// </summary>
public static class IncidentSeed
{
    public static void Apply(NetworkStore store, DateTime now)
    {
        // ---- NEW: 8 báo cáo sinh viên ----
        Student(store, now, "SC-1011", IncidentObjectType.Vehicle, "XE-062", "HUB-01", "Phanh bất thường", "Phanh trước bó cứng khi xuống dốc.", PriorityLevel.High, 8, "SV-2414064");
        Student(store, now, "SC-1012", IncidentObjectType.Vehicle, "XE-101", "HUB-01", "Yên xe rách", "Yên xe bong tróc, không êm.", PriorityLevel.Low, 25, "SV-2414001");
        Student(store, now, "SC-1013", IncidentObjectType.Vehicle, "XE-102", "HUB-01", "Đèn pha không sáng", "Bật công tắc nhưng đèn pha không lên.", PriorityLevel.Low, 40, "SV-2414002");
        Student(store, now, "SC-1014", IncidentObjectType.ChargingPoint, "CS-12-02", "HUB-12", "Đầu nối sạc lỏng", "Đầu nối không giữ được phích sạc.", PriorityLevel.High, 12, "SV-2414003");
        Student(store, now, "SC-1015", IncidentObjectType.Vehicle, "XE-103", "HUB-05", "Phanh kêu", "Phanh sau phát tiếng kêu khi giảm tốc.", PriorityLevel.Medium, 33, "SV-2414004");
        Student(store, now, "SC-1017", IncidentObjectType.Vehicle, "XE-087", "HUB-08", "Khóa xe không phản hồi", "Quét QR nhưng khóa không mở sau 30 giây.", PriorityLevel.Medium, 6, "SV-2414005");
        Student(store, now, "SC-1018", IncidentObjectType.Vehicle, "XE-104", "HUB-06", "Khóa kẹt", "Chìa khóa cơ bị kẹt trong ổ.", PriorityLevel.Medium, 50, "SV-2414006");
        Student(store, now, "SC-1023", IncidentObjectType.Vehicle, "XE-204", "HUB-08", "Lỗi bộ điều khiển", "Xe không nhận lệnh ga, đèn báo lỗi nhấp nháy.", PriorityLevel.High, 5, "SV-2414007");

        // ---- NEW: 4 cảnh báo IoT ----
        Sensor(store, now, "SC-1016", IncidentObjectType.ChargingPoint, "CS-04-03", "HUB-04", "Nhiệt độ cổng sạc cao", "Cảm biến ghi nhận 68°C, vượt ngưỡng an toàn.", PriorityLevel.High, 15);
        Sensor(store, now, "SC-1020", IncidentObjectType.ChargingPoint, "CS-09-02", "HUB-09", "Cổng sạc quá nhiệt", "Cổng sạc báo 74°C và tự ngắt.", PriorityLevel.High, 10);
        Sensor(store, now, "SC-1021", IncidentObjectType.Hub, "HUB-07", "HUB-07", "Cảnh báo cảm biến chập chờn", "Hub mất gói telemetry 2 chu kỳ liên tiếp.", PriorityLevel.Medium, 28);
        Sensor(store, now, "SC-1024", IncidentObjectType.Hub, "HUB-08", "HUB-08", "Hub chạm ngưỡng 90%", "Hub KTX Khu B đạt 54/60 chỗ (90%) — nguy cơ quá tải.", PriorityLevel.Critical, 3);

        // ---- UNDER_CHECK: 5 xe + 2 cổng ----
        Sensor(store, now, "SC-1019", IncidentObjectType.ChargingPoint, "CS-08-04", "HUB-08", "Mất kết nối cảm biến", "Cổng CS-08-04 không phản hồi 14 phút.", PriorityLevel.Medium, 14, IncidentStatus.UnderCheck);
        Sensor(store, now, "SC-1022", IncidentObjectType.Vehicle, "XE-118", "HUB-03", "Pin yếu 18%", "Xe cần được ưu tiên sạc theo UC-SYS-01.", PriorityLevel.Medium, 20, IncidentStatus.UnderCheck);
        Student(store, now, "SC-1025", IncidentObjectType.Vehicle, "XE-105", "HUB-01", "Bánh trước rung", "Bánh trước rung khi chạy trên 15 km/h.", PriorityLevel.Medium, 60, "SV-2414008", IncidentStatus.UnderCheck);
        Student(store, now, "SC-1026", IncidentObjectType.Vehicle, "XE-106", "HUB-02", "Động cơ kêu", "Động cơ phát tiếng kêu lạ khi tăng tốc.", PriorityLevel.Medium, 75, "SV-2414009", IncidentStatus.UnderCheck);
        Sensor(store, now, "SC-1027", IncidentObjectType.Vehicle, "XE-107", "HUB-02", "Pin tụt nhanh", "Pin giảm 20% trong 10 phút.", PriorityLevel.Medium, 90, IncidentStatus.UnderCheck);
        Student(store, now, "SC-1028", IncidentObjectType.Vehicle, "XE-108", "HUB-03", "Cốp không mở", "Cốp xe không mở được bằng ứng dụng.", PriorityLevel.Low, 110, "SV-2414010", IncidentStatus.UnderCheck);
        Sensor(store, now, "SC-1029", IncidentObjectType.ChargingPoint, "CS-08-07", "HUB-08", "Quá nhiệt 72°C", "Cổng CS-08-07 ngắt khẩn cấp do quá nhiệt.", PriorityLevel.High, 120, IncidentStatus.UnderCheck);

        // ---- IN_REPAIR: 5 ----
        Student(store, now, "SC-1030", IncidentObjectType.Vehicle, "XE-109", "HUB-03", "Đứt phanh", "Dây phanh sau đứt, đã tạm khoá xe.", PriorityLevel.High, 180, "SV-2414011", IncidentStatus.InRepair);
        Student(store, now, "SC-1031", IncidentObjectType.Vehicle, "XE-110", "HUB-05", "Cháy đèn", "Đèn hậu bị cháy, đã thay.", PriorityLevel.Medium, 240, "SV-2414012", IncidentStatus.InRepair);
        Sensor(store, now, "SC-1032", IncidentObjectType.Vehicle, "XE-111", "HUB-04", "Mất kết nối", "Xe mất kết nối 3 chu kỳ, kỹ thuật viên đang kiểm tra.", PriorityLevel.High, 300, IncidentStatus.InRepair);
        Sensor(store, now, "SC-1033", IncidentObjectType.ChargingPoint, "CS-02-02", "HUB-02", "Không cấp điện", "Cổng không cấp nguồn dù đã ACK.", PriorityLevel.High, 360, IncidentStatus.InRepair);
        Student(store, now, "SC-1034", IncidentObjectType.ChargingPoint, "CS-12-05", "HUB-12", "Đầu sạc hỏng", "Đầu sạc nứt, đã tạm khoá cổng.", PriorityLevel.Medium, 420, "SV-2414013", IncidentStatus.InRepair);

        // Nhật ký hero SC-1024 khớp mô tả Sub 2 §6.2 (phát hiện → cảnh báo → đề xuất điều chuyển).
        var hero = store.Incidents.First(i => i.IncidentId == "SC-1024");
        hero.Logs.Add(new IncidentLogEntry
        {
            At = now.AddMinutes(-2),
            Message = "Gửi cảnh báo tới điều phối viên: HUB-08 đạt 90% sức chứa.",
            Actor = "Hệ thống"
        });
        hero.Logs.Add(new IncidentLogEntry
        {
            At = now.AddMinutes(-1),
            Message = "Đề xuất điều chuyển 12 xe sang Hub Nhà học A1.",
            Actor = "Hệ thống"
        });
    }

    private static void Student(
        NetworkStore store,
        DateTime now,
        string id,
        IncidentObjectType objectType,
        string objectCode,
        string hubId,
        string problem,
        string description,
        PriorityLevel priority,
        int minutesAgo,
        string reporterId,
        IncidentStatus status = IncidentStatus.New)
    {
        var record = Build(store, now, id, objectType, objectCode, hubId, IncidentSource.StudentReport,
            problem, description, priority, minutesAgo, status);
        record.ReporterIds.Add(reporterId);
    }

    private static void Sensor(
        NetworkStore store,
        DateTime now,
        string id,
        IncidentObjectType objectType,
        string objectCode,
        string hubId,
        string problem,
        string description,
        PriorityLevel priority,
        int minutesAgo,
        IncidentStatus status = IncidentStatus.New)
    {
        Build(store, now, id, objectType, objectCode, hubId, IncidentSource.IotSensor,
            problem, description, priority, minutesAgo, status);
    }

    private static IncidentRecord Build(
        NetworkStore store,
        DateTime now,
        string id,
        IncidentObjectType objectType,
        string objectCode,
        string hubId,
        IncidentSource source,
        string problem,
        string description,
        PriorityLevel priority,
        int minutesAgo,
        IncidentStatus status)
    {
        var createdAt = now.AddMinutes(-minutesAgo);
        var record = new IncidentRecord
        {
            IncidentId = id,
            ObjectType = objectType,
            ObjectCode = objectCode,
            HubId = hubId,
            Source = source,
            ProblemType = problem,
            Description = description,
            CreatedAt = createdAt,
            Status = status,
            Priority = priority
        };
        record.Logs.Add(new IncidentLogEntry
        {
            At = createdAt,
            Message = $"Ghi nhận sự cố: {problem}.",
            Actor = source == IncidentSource.StudentReport ? "Sinh viên" : "Cảm biến IoT"
        });
        store.Incidents.Add(record);
        return record;
    }
}
