using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Services
{
    /// <summary>
    /// Báo lỗi của sinh viên (UC-10) đi thẳng vào hàng đợi sự cố của Trung tâm điều hành (IncidentManager, DC-54):
    /// báo cáo trùng được gộp, thiết bị chuyển "Đang kiểm tra", mã báo cáo là mã sự cố.
    /// </summary>
    public class IncidentService
    {
        private readonly StudentContext Ctx;

        public IncidentService(StudentContext ctx) => Ctx = ctx;

        private MockDataStore Store => Ctx.Store;

        public Incident? FindOpenIncident(string objectCode) =>
            Ctx.Network.Incidents.FindOpenIncident(objectCode.Trim()) is { } record ? Store.View(record) : null;

        public OpResult<Incident> ReceiveStudentReport(ReportObjectKind kind, string objectCode, string? hubId, int? pointNumber, string problemType, string description, double? photoMb)
        {
            if (photoMb.HasValue && photoMb.Value > DemoConfig.MaxPhotoMb)
                return OpResult<Incident>.Fail("PhotoTooLarge", $"Ảnh {photoMb.Value:0.0} MB vượt giới hạn {DemoConfig.MaxPhotoMb:0} MB. Vui lòng chọn ảnh nhỏ hơn.");
            if (string.IsNullOrWhiteSpace(objectCode) && kind == ReportObjectKind.Vehicle)
                return OpResult<Incident>.Fail("MissingCode", "Vui lòng nhập mã đối tượng cần báo lỗi.");
            if (string.IsNullOrWhiteSpace(problemType))
                return OpResult<Incident>.Fail("MissingType", "Vui lòng chọn loại sự cố.");

            CoreDomain.IncidentObjectType type;
            string code;
            if (kind == ReportObjectKind.Vehicle)
            {
                var vehicle = Store.GetVehicle(objectCode);
                if (vehicle == null) return OpResult<Incident>.Fail("UnknownObject", $"Không tìm thấy phương tiện {objectCode.Trim()}.");
                type = CoreDomain.IncidentObjectType.Vehicle;
                code = vehicle.Code;
            }
            else
            {
                var hub = Store.FindHub(hubId);
                var point = hub?.Points.FirstOrDefault(p => p.Number == pointNumber);
                if (hub == null || point == null)
                    return OpResult<Incident>.Fail("UnknownObject", "Vui lòng chọn Hub và cổng sạc cần báo lỗi.");
                type = CoreDomain.IncidentObjectType.ChargingPoint;
                code = point.Id;
            }

            var existing = FindOpenIncident(code);
            var photoBytes = photoMb.HasValue ? (long)(photoMb.Value * 1024 * 1024) : 0;
            CoreDomain.IncidentRecord record;
            try
            {
                record = Ctx.Gateway.ReportProblem(Store.StudentId, type, code, problemType, description, photoBytes);
            }
            catch (Exception ex) when (ex is CoreDomain.IllegalArgumentException or CoreDomain.FileTooLargeException)
            {
                return OpResult<Incident>.Fail("Rejected", ex.Message);
            }

            var incident = Store.View(record);
            var shown = kind == ReportObjectKind.Vehicle ? code : DescribePoint(hubId, pointNumber);
            if (existing != null)
            {
                Ctx.Notifications.Send("Đã tiếp nhận báo lỗi", $"{shown} đã có sự cố đang xử lý ({incident.Code}). Báo cáo của bạn đã được gộp vào sự cố này.", Tone.Info);
                return new OpResult<Incident> { Value = incident, Message = "Linked" };
            }
            Ctx.Notifications.Send("Đã tiếp nhận báo lỗi", $"Mã báo cáo {incident.Code} · {shown} · Người điều hành đang xử lý.", Tone.Info);
            return OpResult<Incident>.Success(incident);
        }

        private string DescribePoint(string? hubId, int? pointNumber) =>
            Store.FindHub(hubId) is { } hub ? $"{hub.ShortName} · Cổng #{pointNumber}" : $"Cổng #{pointNumber}";
    }
}
