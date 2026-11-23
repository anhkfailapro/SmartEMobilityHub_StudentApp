using System.Windows.Media;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Operator.Models;

internal static class Texts
{
    public static string Status(IncidentStatus status) => status switch
    {
        IncidentStatus.New => "Mới",
        IncidentStatus.UnderCheck => "Đang kiểm tra",
        IncidentStatus.Locked => "Đã khoá",
        IncidentStatus.InRepair => "Đang sửa",
        IncidentStatus.Resolved => "Đã xử lý",
        _ => "Đã đóng"
    };

    public static (Brush Background, Brush Foreground) StatusTone(IncidentStatus status) => status switch
    {
        IncidentStatus.New => (Brush("#FEF3C7"), Brush("#B45309")),
        IncidentStatus.UnderCheck => (Brush("#E0F2FE"), Brush("#0369A1")),
        IncidentStatus.Locked => (Brush("#FEE2E2"), Brush("#B91C1C")),
        IncidentStatus.InRepair => (Brush("#EDE9FE"), Brush("#6D28D9")),
        IncidentStatus.Resolved => (Brush("#D1FAE5"), Brush("#047857")),
        _ => (Brush("#F1F5F9"), Brush("#64748B"))
    };

    public static string Priority(PriorityLevel priority) => priority switch
    {
        PriorityLevel.Critical => "Khẩn cấp",
        PriorityLevel.High => "Cao",
        PriorityLevel.Medium => "Trung bình",
        _ => "Thấp"
    };

    public static Brush PriorityBrush(PriorityLevel priority) => priority switch
    {
        PriorityLevel.Critical => Brush("#DC2626"),
        PriorityLevel.High => Brush("#EA580C"),
        PriorityLevel.Medium => Brush("#0284C7"),
        _ => Brush("#64748B")
    };

    public static string Source(IncidentSource source) => source switch
    {
        IncidentSource.StudentReport => "Sinh viên",
        IncidentSource.IotSensor => "Cảm biến IoT",
        _ => "Hệ thống"
    };

    public static string Object(IncidentObjectType type) => type switch
    {
        IncidentObjectType.Vehicle => "Xe",
        IncidentObjectType.ChargingPoint => "Cổng",
        _ => "Hub"
    };

    public static string Ago(DateTime then, DateTime now)
    {
        var delta = now - then;
        if (delta.TotalMinutes < 1) return "Vừa xong";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} phút trước";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} giờ trước";
        return then.ToString("dd/MM HH:mm");
    }

    public static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}

public sealed class IncidentLogRow
{
    public string TimeText { get; init; } = "";
    public string Message { get; init; } = "";
    public string Actor { get; init; } = "";
}

public sealed class IncidentRow
{
    public string Id { get; }
    public string Code { get; }
    public string Title { get; }
    public string Meta { get; }
    public string Description { get; }
    public string ReporterText { get; }
    public string CreatedText { get; }
    public string HubId { get; }
    public string StatusText { get; }
    public Brush StatusBackground { get; }
    public Brush StatusForeground { get; }
    public string PriorityText { get; }
    public Brush PriorityBrush { get; }
    public IncidentStatus StatusRaw { get; }
    public List<IncidentLogRow> Logs { get; }

    public IncidentRow(IncidentRecord incident, NetworkStore store, DateTime now)
    {
        Id = incident.IncidentId;
        Code = incident.ObjectCode;
        Title = $"{Texts.Object(incident.ObjectType)} {incident.ObjectCode} · {incident.ProblemType}";
        HubId = incident.HubId;

        var hub = store.FindHub(incident.HubId);
        Meta = $"{hub?.ShortName ?? incident.HubId} · {Texts.Source(incident.Source)}";
        Description = incident.Description;
        ReporterText = incident.ReporterIds.Count > 0
            ? string.Join(" · ", incident.ReporterIds)
            : Texts.Source(incident.Source);
        CreatedText = Texts.Ago(incident.CreatedAt, now);

        StatusText = Texts.Status(incident.Status);
        (StatusBackground, StatusForeground) = Texts.StatusTone(incident.Status);
        PriorityText = Texts.Priority(incident.Priority);
        PriorityBrush = Texts.PriorityBrush(incident.Priority);
        StatusRaw = incident.Status;

        Logs = incident.Logs
            .OrderBy(l => l.At)
            .Select(l => new IncidentLogRow
            {
                TimeText = l.At.ToString("HH:mm"),
                Message = l.Message,
                Actor = l.Actor
            })
            .ToList();
    }
}

public sealed class MaintenanceRow
{
    public string TaskId { get; }
    public string StatusText { get; }
    public string TechText { get; }
    public string SlaText { get; }
    public Brush SlaBrush { get; }

    public MaintenanceRow(MaintenanceTask task, DateTime now)
    {
        TaskId = task.TaskId;
        StatusText = task.Status switch
        {
            MaintenanceStatus.Assigned => "Đã phân công",
            MaintenanceStatus.InProgress => "Đang thực hiện",
            MaintenanceStatus.Completed => "Hoàn thành",
            _ => "Đã huỷ"
        };

        TechText = task.AssignedTech is null
            ? "Chưa gán kỹ thuật viên"
            : $"{task.AssignedTech.TechId} · {task.AssignedTech.Name}";

        if (task.Status == MaintenanceStatus.Completed)
        {
            SlaText = "Đã xong";
            SlaBrush = Texts.Brush("#10B981");
            return;
        }

        var remaining = task.SlaDeadline - now;
        if (remaining < TimeSpan.Zero)
        {
            SlaText = $"Quá hạn {-remaining.TotalMinutes:0} phút";
            SlaBrush = Texts.Brush("#DC2626");
        }
        else
        {
            SlaText = remaining.TotalHours >= 1
                ? $"Còn {remaining.Hours}h {remaining.Minutes:00}p"
                : $"Còn {remaining.TotalMinutes:0} phút";
            SlaBrush = Texts.Brush("#64748B");
        }
    }
}

public sealed class TechnicianRow
{
    public string Name { get; }
    public string Team { get; }
    public string DistanceText { get; }
    public string AvailableText { get; }
    public Brush AvailableBrush { get; }

    public TechnicianRow(Technician technician, MobilityHub? hub)
    {
        Name = technician.Name;
        Team = technician.Team;
        DistanceText = hub is null ? "" : $"{technician.DistanceToHub(hub):0.0} km";
        AvailableText = technician.IsAvailable() ? "Sẵn sàng" : "Đang bận";
        AvailableBrush = Texts.Brush(technician.IsAvailable() ? "#10B981" : "#CBD5E1");
    }
}

public sealed class AlertRow
{
    public string TimeText { get; }
    public string Title { get; }
    public string Message { get; }
    public string DeliveryText { get; }
    public Brush DeliveryBrush { get; }

    public AlertRow(Alert alert)
    {
        TimeText = alert.CreatedAt.ToString("HH:mm:ss");
        Title = alert.Type switch
        {
            AlertType.HubOverload => "Quá tải Hub",
            AlertType.StaleData => "Dữ liệu cũ",
            AlertType.DeviceOffline => "Mất kết nối",
            AlertType.PowerLimit => "Vượt công suất",
            _ => "Cổng sạc lỗi"
        };
        Message = alert.Message;
        DeliveryText = alert.Delivered ? "Đã gửi" : "Chờ gửi lại";
        DeliveryBrush = Texts.Brush(alert.Delivered ? "#10B981" : "#F59E0B");
    }
}

public sealed class QueueRow
{
    public string PositionText { get; }
    public string Vehicle { get; }
    public string BatteryText { get; }
    public string ScoreText { get; }
    public string StatusText { get; }

    public QueueRow(ChargingRequest request, int index)
    {
        PositionText = $"#{index + 1}";
        Vehicle = request.VehicleId;
        BatteryText = $"{request.BatteryLevel}%";
        ScoreText = $"{request.PriorityScore:0.0}";
        StatusText = request.AssignedPortId ?? "Chờ cổng";
    }
}

public sealed class FilterOption
{
    public FilterOption(string label, params IncidentStatus[] statuses)
    {
        Label = label;
        Statuses = statuses;
    }

    public string Label { get; }
    public IReadOnlyList<IncidentStatus> Statuses { get; }

    public bool Matches(IncidentStatus status) => Statuses.Count == 0 || Statuses.Contains(status);
}
public sealed class HubRow
{
    public string HubId { get; }
    public string Name { get; }
    public string StatusText { get; }
    public Brush StatusBrush { get; }
    public string LoadText { get; }
    public string UtilizationText { get; }
    public Brush UtilizationBrush { get; }
    public string FreeSlotsText { get; }
    public string FreeChargersText { get; }
    public string ImbalanceTag { get; }

    public HubRow(MobilityHub hub)
    {
        HubId = hub.Id;
        Name = string.IsNullOrWhiteSpace(hub.ShortName) ? hub.Name : $"{hub.Id} · {hub.ShortName}";

        var isOnline = hub.Status == HubStatus.Online && hub.Connection == ConnectionStatus.Online;
        StatusText = hub.Status == HubStatus.Suspended
            ? "Tạm dừng"
            : isOnline ? "Hoạt động" : "Mất kết nối / Offline";
        StatusBrush = Texts.Brush(hub.Status == HubStatus.Suspended ? "#DC2626" : isOnline ? "#10B981" : "#F59E0B");

        LoadText = $"{hub.OccupiedParkingSlots}/{hub.ParkingCapacity} chỗ";
        var utilPct = hub.MaxUtilization * 100.0;
        UtilizationText = $"{utilPct:0}%";
        UtilizationBrush = Texts.Brush(utilPct >= 90 ? "#DC2626" : utilPct >= 80 ? "#EA580C" : "#0284C7");

        FreeSlotsText = $"Trống {hub.FreeParkingSlots} chỗ";
        var freeChargers = hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Available);
        FreeChargersText = $"{freeChargers}/{hub.UsableChargingPorts} sạc trống";

        ImbalanceTag = hub.ParkingUtilization >= 0.85
            ? "Dư xe / Cao"
            : hub.ParkingUtilization <= 0.35
                ? "Thiếu xe"
                : "Cân bằng";
    }
}

public sealed class DispatchOrderRow
{
    public string OrderId { get; }
    public string RouteText { get; }
    public string VehiclesText { get; }
    public string MetaText { get; }
    public string StatusText { get; }
    public Brush StatusBrush { get; }
    public DispatchOrderStatus StatusRaw { get; }

    public DispatchOrderRow(DispatchOrder order)
    {
        OrderId = order.OrderId;
        RouteText = $"{order.SourceHubId} → {order.TargetHubId}";
        VehiclesText = $"{order.VehicleIds.Count} xe ({string.Join(", ", order.VehicleIds.Take(3))}{(order.VehicleIds.Count > 3 ? "…" : "")})";
        MetaText = $"{order.EstimatedDistanceKm:0.0} km · ~{order.EstimatedDurationMinutes:0} phút · {order.StaffId}";
        StatusRaw = order.Status;

        (StatusText, var hex) = order.Status switch
        {
            DispatchOrderStatus.Pending => ("Chờ xuất phát", "#F59E0B"),
            DispatchOrderStatus.InProgress => ("Đang điều phối", "#2563EB"),
            DispatchOrderStatus.Completed => ("Hoàn tất", "#10B981"),
            _ => ("Đã huỷ", "#64748B")
        };
        StatusBrush = Texts.Brush(hex);
    }
}
