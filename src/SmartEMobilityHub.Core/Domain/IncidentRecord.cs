namespace SmartEMobilityHub.Core.Domain;

/// <summary>Một dòng nhật ký xử lý sự cố (hiển thị ở cột phải màn hình Incident).</summary>
public sealed class IncidentLogEntry
{
    public DateTime At { get; init; }
    public string Message { get; init; } = "";
    public string Actor { get; init; } = "Hệ thống";

    public IncidentLogEntry Clone() => new() { At = At, Message = Message, Actor = Actor };
}

/// <summary>
/// Sự cố vận hành (Bảng 42). Vòng đời và quy tắc chuyển trạng thái nằm ở IncidentManager (Bảng 41).
/// </summary>
public sealed class IncidentRecord
{
    public string IncidentId { get; init; } = "";
    public IncidentObjectType ObjectType { get; init; }
    public string ObjectCode { get; init; } = "";
    public string HubId { get; init; } = "";
    public IncidentSource Source { get; init; }

    public List<string> ReporterIds { get; } = new();
    public string ProblemType { get; init; } = "";
    public string Description { get; set; } = "";

    public DateTime CreatedAt { get; init; }
    public IncidentStatus Status { get; set; } = IncidentStatus.New;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    public List<IncidentLogEntry> Logs { get; } = new();

    /// <summary>Bảng 43: sự cố còn mở khi chưa RESOLVED/CLOSED.</summary>
    public bool IsOpen() => Status != IncidentStatus.Resolved && Status != IncidentStatus.Closed;

    /// <summary>Bảng 43: gộp báo cáo trùng — thêm người báo (không trùng) và nối mô tả.</summary>
    public void AddReport(string userId, string description)
    {
        if (!string.IsNullOrWhiteSpace(userId) && !ReporterIds.Contains(userId))
            ReporterIds.Add(userId);

        if (!string.IsNullOrWhiteSpace(description))
        {
            Description = string.IsNullOrEmpty(Description)
                ? description
                : Description + " | " + description;
        }
    }

    public IncidentRecord Clone()
    {
        var clone = new IncidentRecord
        {
            IncidentId = IncidentId,
            ObjectType = ObjectType,
            ObjectCode = ObjectCode,
            HubId = HubId,
            Source = Source,
            ProblemType = ProblemType,
            Description = Description,
            CreatedAt = CreatedAt,
            Status = Status,
            Priority = Priority
        };
        clone.ReporterIds.AddRange(ReporterIds);
        foreach (var log in Logs) clone.Logs.Add(log.Clone());
        return clone;
    }
}
