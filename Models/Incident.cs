using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Models
{
    /// <summary>
    /// Báo lỗi của sinh viên = sự cố trong hàng đợi của Trung tâm điều hành (<see cref="CoreDomain.IncidentRecord"/>).
    /// Mã báo cáo chính là mã sự cố (SC-…), trạng thái cập nhật ngay khi Operator xử lý (DC-54).
    /// </summary>
    public sealed class Incident
    {
        public Incident(CoreDomain.IncidentRecord core) => Core = core;

        public CoreDomain.IncidentRecord Core { get; }

        public string Code => Core.IncidentId;
        public ReportObjectKind ObjectKind =>
            Core.ObjectType == CoreDomain.IncidentObjectType.Vehicle ? ReportObjectKind.Vehicle : ReportObjectKind.ChargingPoint;
        public string ObjectCode => Core.ObjectCode;
        public string HubId => Core.HubId;
        public string ProblemType => Core.ProblemType;
        public string Description => Core.Description;
        public IncidentStatus Status => Core.Status;
        public IncidentSource Source => Core.Source;
        public DateTime CreatedAt => Core.CreatedAt;

        /// <summary>Số người đã báo (báo cáo trùng được gộp vào cùng sự cố).</summary>
        public int ReportCount => Math.Max(1, Core.ReporterIds.Count);
    }
}
