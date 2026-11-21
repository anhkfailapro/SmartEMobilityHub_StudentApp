namespace SmartEMobilityHub.Core.Domain;

/// <summary>Phiếu bảo trì (Bảng 44) với các chuyển trạng thái ở Bảng 45.</summary>
public sealed class MaintenanceTask
{
    public string TaskId { get; init; } = "";
    public string IncidentId { get; init; } = "";
    public Technician? AssignedTech { get; set; }
    public DateTime SlaDeadline { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Assigned;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public string? Note { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>Bảng 45: gán kỹ thuật viên, đặt lại hạn SLA theo mức ưu tiên (giả định A9).</summary>
    public void Assign(Technician tech, DateTime now, TimeSpan sla)
    {
        if (tech is null) throw new IllegalArgumentException("Kỹ thuật viên không hợp lệ.");
        AssignedTech = tech;
        SlaDeadline = now + sla;
    }

    /// <summary>Bảng 45: ASSIGNED → IN_PROGRESS; sai trạng thái thì báo lỗi.</summary>
    public void Start()
    {
        if (Status != MaintenanceStatus.Assigned)
            throw new IllegalStateException($"Không thể bắt đầu phiếu ở trạng thái {Status}.");
        Status = MaintenanceStatus.InProgress;
    }

    /// <summary>Bảng 45: IN_PROGRESS → COMPLETED, ghi thời điểm hoàn thành.</summary>
    public void Complete(string note, DateTime now)
    {
        if (Status != MaintenanceStatus.InProgress)
            throw new IllegalStateException($"Không thể hoàn thành phiếu ở trạng thái {Status}.");
        Status = MaintenanceStatus.Completed;
        CompletedAt = now;
        Note = note;
    }

    /// <summary>Bảng 45: huỷ phiếu chưa hoàn thành, lưu lý do.</summary>
    public void Cancel(string reason)
    {
        if (Status == MaintenanceStatus.Completed)
            throw new IllegalStateException("Phiếu đã hoàn thành, không thể huỷ.");
        Status = MaintenanceStatus.Cancelled;
        CancelReason = reason;
    }

    public TimeSpan GetRemainingSla(DateTime now) => SlaDeadline - now;

    /// <summary>Bảng 45: quá hạn SLA và chưa COMPLETED.</summary>
    public bool IsSlaBreached(DateTime now) => Status != MaintenanceStatus.Completed && now > SlaDeadline;

    public MaintenanceTask Clone()
    {
        var clone = new MaintenanceTask
        {
            TaskId = TaskId,
            IncidentId = IncidentId,
            SlaDeadline = SlaDeadline,
            Status = Status,
            CreatedAt = CreatedAt,
            CompletedAt = CompletedAt,
            Note = Note,
            CancelReason = CancelReason
        };
        if (AssignedTech is not null) clone.AssignedTech = AssignedTech.Clone();
        return clone;
    }
}
