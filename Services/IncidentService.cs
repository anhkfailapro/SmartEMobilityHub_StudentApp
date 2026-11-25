using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class IncidentService
    {
        private static MockDataStore Store => DemoApp.Store;

        public Incident? FindOpenIncident(string objectCode) =>
            Store.Incidents.FirstOrDefault(i => string.Equals(i.ObjectCode, objectCode, StringComparison.OrdinalIgnoreCase)
                                                && i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed);

        public OpResult<Incident> ReceiveStudentReport(ReportObjectKind kind, string objectCode, string? hubId, int? pointNumber, string problemType, string description, double? photoMb)
        {
            if (photoMb.HasValue && photoMb.Value > DemoConfig.MaxPhotoMb)
                return OpResult<Incident>.Fail("PhotoTooLarge", $"Ảnh {photoMb.Value:0.0} MB vượt giới hạn {DemoConfig.MaxPhotoMb:0} MB. Vui lòng chọn ảnh nhỏ hơn.");
            if (string.IsNullOrWhiteSpace(objectCode))
                return OpResult<Incident>.Fail("MissingCode", "Vui lòng nhập mã đối tượng cần báo lỗi.");
            if (string.IsNullOrWhiteSpace(problemType))
                return OpResult<Incident>.Fail("MissingType", "Vui lòng chọn loại sự cố.");

            Vehicle? vehicle = null;
            if (kind == ReportObjectKind.Vehicle)
            {
                vehicle = Store.GetVehicle(objectCode);
                if (vehicle == null) return OpResult<Incident>.Fail("UnknownObject", $"Không tìm thấy phương tiện {objectCode.Trim()}.");
                objectCode = vehicle.Code;
                hubId = vehicle.HubId ?? Store.ActiveTrip?.StartHubId ?? "H1";
            }

            var open = FindOpenIncident(objectCode);
            if (open != null)
            {
                open.ReportCount++;
                DemoApp.Link.OnProblemReported(open, pointNumber);
                DemoApp.Notifications.Send("Đã tiếp nhận báo lỗi", $"{objectCode} đã có sự cố đang xử lý ({open.Code}). Báo cáo của bạn đã được gộp vào sự cố này.", Tone.Info);
                return new OpResult<Incident> { Value = open, Message = "Linked" };
            }

            var incident = new Incident
            {
                Code = Store.NextReportCode(),
                ObjectKind = kind,
                ObjectCode = objectCode,
                HubId = hubId ?? "H1",
                ProblemType = problemType,
                Description = description,
                Status = IncidentStatus.UnderCheck,
                Source = IncidentSource.StudentReport,
                CreatedAt = DemoApp.Clock.Now
            };
            Store.Incidents.Add(incident);
            DemoApp.Link.OnProblemReported(incident, pointNumber);

            if (vehicle != null && vehicle.Status != VehicleStatus.InUse) vehicle.Status = VehicleStatus.UnderCheck;
            if (kind == ReportObjectKind.ChargingPoint && pointNumber.HasValue)
            {
                var point = Store.GetHub(incident.HubId).Points.FirstOrDefault(p => p.Number == pointNumber.Value);
                if (point != null) point.Status = PointStatus.OutOfService;
            }
            DemoApp.Notifications.Send("Đã tiếp nhận báo lỗi", $"Mã báo cáo {incident.Code} · {objectCode} · Người điều hành đang xử lý.", Tone.Info);
            return OpResult<Incident>.Success(incident);
        }

        public Incident CreateUnlockTimeoutIncident(string vehicleCode, string hubId)
        {
            var incident = new Incident
            {
                Code = Store.NextReportCode(),
                ObjectKind = ReportObjectKind.Vehicle,
                ObjectCode = vehicleCode,
                HubId = hubId,
                ProblemType = "Không mở khóa được",
                Description = $"Không nhận được xác nhận mở khóa sau {DemoConfig.UnlockTimeoutSeconds} giây.",
                Status = IncidentStatus.New,
                Source = IncidentSource.UnlockTimeout,
                CreatedAt = DemoApp.Clock.Now
            };
            Store.Incidents.Add(incident);
            return incident;
        }
    }
}
