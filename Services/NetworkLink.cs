using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using CoreDomain = SmartEMobilityHub.Core.Domain;
using CoreServices = SmartEMobilityHub.Core.Services;
using OperatorDemo = SmartEMobilityHub.Operator.OperatorDemo;
using OperatorLoginWindow = SmartEMobilityHub.Operator.LoginWindow;
using OperatorMainWindow = SmartEMobilityHub.Operator.MainWindow;

namespace SmartEMobilityHub.Services
{
    /// <summary>
    /// Liên thông app Sinh viên với Trung tâm điều hành trong cùng một tiến trình.
    /// App Sinh viên vẫn giữ dữ liệu hiển thị riêng (MockDataStore), còn mạng lưới chung là NetworkStore của Core
    /// (bên trong <see cref="OperatorDemo"/>). Lớp này làm hai việc:
    ///  1. Đẩy thao tác của sinh viên sang Core qua <see cref="CoreServices.StudentAppGateway"/> (đặt / huỷ / nhận / trả xe,
    ///     báo lỗi, yêu cầu sạc, đặt chỗ đỗ).
    ///  2. Mỗi nhịp đồng hồ, đọc ngược thay đổi do Operator tạo ra (tạm dừng Hub, khoá xe, điều phối xe,
    ///     cấp cổng sạc, xử lý sự cố) và cập nhật app Sinh viên.
    /// Lỗi ở phía Core không bao giờ làm hỏng luồng của sinh viên: chỉ ghi vào nhật ký hệ thống của Operator.
    /// </summary>
    public sealed class NetworkLink
    {
        /// <summary>Hub của app Sinh viên ↔ Hub của mạng lưới Core.</summary>
        public static readonly IReadOnlyDictionary<string, string> HubMap = new Dictionary<string, string>
        {
            ["H1"] = "HUB-01", // Ga Metro
            ["H2"] = "HUB-04", // Thư viện Trung tâm
            ["H3"] = "HUB-02", // KTX Khu A
            ["H4"] = "HUB-11"  // Nhà văn hóa SV
        };

        private readonly Dictionary<string, string> _incidentIds = new();
        private OperatorMainWindow? _window;
        private CoreServices.AuthService? _operatorAuth;

        public OperatorDemo Demo { get; private set; } = null!;

        public CoreServices.StudentAppGateway Gateway => Demo.StudentGateway;

        public bool IsOperatorWindowOpen => _window is not null;

        private static MockDataStore Store => DemoApp.Store;
        private static DateTime Now => DemoApp.Clock.Now;
        private static string UserId => string.IsNullOrEmpty(Store.StudentId) ? "SV" : Store.StudentId;

        /// <summary>Tạo mạng lưới mới cùng giờ với app Sinh viên và đưa toàn bộ xe của app Sinh viên vào.</summary>
        public void Reset()
        {
            Demo = new OperatorDemo(Now);
            _incidentIds.Clear();
            foreach (var v in Store.Vehicles)
            {
                Try(() => Gateway.EnsureVehicle(v.Code, MapType(v.Type), CoreHub(v.HubId), v.Slot, v.Battery, MapStatus(v.Status)));
            }
            _window?.AttachDemo(Demo);
        }

        // ---------- Mở Trung tâm điều hành dùng chung dữ liệu ----------

        /// <summary>Mở cửa sổ đăng nhập Operator; đăng nhập đúng tài khoản Operator mới vào được bảng điều khiển (NFR 2.2.4).</summary>
        public void OpenOperatorDashboard()
        {
            if (_window is not null)
            {
                _window.Activate();
                return;
            }

            // Phiên đăng nhập riêng cho Operator để không đè phiên của sinh viên.
            _operatorAuth ??= CreateOperatorAuth();
            _operatorAuth.Logout();
            var login = new OperatorLoginWindow(_operatorAuth) { Owner = Application.Current.MainWindow };
            login.LoggedIn += account =>
            {
                var window = new OperatorMainWindow(Demo);
                window.SetCurrentUser(account);
                window.Closed += (_, _) => _window = null;
                _window = window;
                window.Show();
                login.Close();
            };
            login.Show();
        }

        private static CoreServices.AuthService CreateOperatorAuth()
        {
            var auth = new CoreServices.AuthService(DemoApp.Clock);
            global::SmartEMobilityHub.Core.Seed.AccountSeed.Apply(auth);
            return auth;
        }

        // ---------- Sinh viên → mạng lưới ----------

        /// <summary>Kiểm tra với mạng lưới trước khi đặt xe. Trả mã lỗi theo quy ước của ReservationService, hoặc "".</summary>
        public string CheckReserve(Vehicle vehicle)
        {
            var hubId = CoreHub(vehicle.HubId);
            if (hubId is not null && Gateway.GetHubBlockReason(hubId) is not null) return "HubSuspended";

            var coreVehicle = Gateway.FindVehicle(vehicle.Code);
            if (coreVehicle is not null &&
                coreVehicle.Status is not (CoreDomain.DeviceStatus.Available or CoreDomain.DeviceStatus.NeedsCharge))
            {
                return "TakenByOther";
            }

            return "";
        }

        public string? HubBlockReason(string studentHubId)
        {
            var hubId = CoreHub(studentHubId);
            return hubId is null ? null : Gateway.GetHubBlockReason(hubId);
        }

        /// <summary>Tình huống UC-03 2a "SV khác đặt xe trước": ghi đặt chỗ của sinh viên khác vào mạng lưới để Operator cũng thấy.</summary>
        public void OnReservedByOtherStudent(Vehicle v)
        {
            var hubId = CoreHub(v.HubId);
            if (hubId is null) return;
            Try(() => Gateway.ReserveVehicle(Store.NextId("VR-KHAC"), "SV-KHAC", v.Code, hubId, Now.AddMinutes(DemoConfig.HoldMinutes)));
        }

        public void OnVehicleReserved(VehicleReservation r)
        {
            var hubId = CoreHub(r.HubId);
            if (hubId is null) return;
            Try(() => Gateway.ReserveVehicle(r.Id, UserId, r.VehicleCode, hubId, r.HoldUntil));
        }

        public void OnReservationCancelled(VehicleReservation r) =>
            Try(() => Gateway.CloseReservation(r.Id, CoreServices.StudentAppGateway.CancelledByStudent));

        public void OnReservationExpired(VehicleReservation r) =>
            Try(() => Gateway.CloseReservation(r.Id, CoreServices.StudentAppGateway.ExpiredOnStudentApp));

        public void OnPickedUp(VehicleReservation r) => Try(() => Gateway.PickUp(r.Id));

        public void OnReturned(Vehicle v, string studentHubId)
        {
            var hubId = CoreHub(studentHubId);
            if (hubId is null) return;
            Try(() => Gateway.ReturnVehicle(v.Code, hubId, v.Slot, v.Battery, v.Status == VehicleStatus.NeedsCharge));
        }

        public void OnUnlockTimeout(VehicleReservation r) =>
            Try(() => Demo.Incidents.ReceiveUnlockTimeout(r.VehicleCode, r.Id));

        public void OnProblemReported(Incident incident, int? pointNumber)
        {
            var hubId = CoreHub(incident.HubId);
            string? objectCode = incident.ObjectKind == ReportObjectKind.Vehicle
                ? incident.ObjectCode
                : hubId is null || pointNumber is null ? null : CorePointId(hubId, pointNumber.Value);
            if (objectCode is null) return;

            var type = incident.ObjectKind == ReportObjectKind.Vehicle
                ? CoreDomain.IncidentObjectType.Vehicle
                : CoreDomain.IncidentObjectType.ChargingPoint;
            Try(() =>
            {
                var record = Gateway.ReportProblem(UserId, type, objectCode, incident.ProblemType, incident.Description);
                _incidentIds[incident.Code] = record.IncidentId;
            });
        }

        public void OnChargingRequested(ChargingRequest request)
        {
            var hubId = CoreHub(request.HubId);
            if (hubId is null) return;
            var battery = Store.GetVehicle(request.VehicleCode)?.Battery ?? 50;
            var neededAt = Store.ActiveTrip is not null ? Now.AddMinutes(30) : Now.AddHours(2);
            Try(() => Gateway.RequestCharging(request.Id, request.VehicleCode, hubId, battery, neededAt));
        }

        public void OnChargingRequestCancelled(ChargingRequest request) => Try(() => Gateway.CancelChargingRequest(request.Id));

        public void OnParkingChanged(string studentHubId, int delta)
        {
            var hubId = CoreHub(studentHubId);
            if (hubId is null) return;
            Try(() => Gateway.ChangeParkingOccupancy(hubId, delta));
        }

        // ---------- Mạng lưới → sinh viên ----------

        /// <summary>
        /// Đồng bộ một nhịp. Trả true nếu dữ liệu app Sinh viên thay đổi (cần vẽ lại màn hình).
        /// Gọi từ DemoApp.Tick mỗi giây.
        /// </summary>
        public bool SyncFromNetwork()
        {
            // Hai bên dùng chung một mốc giờ: tua giờ ở app Sinh viên thì mạng lưới cũng tiến theo.
            if (Now > Demo.Clock.Now) Demo.Clock.Set(Now);

            var changed = false;
            changed |= SyncHubs();
            changed |= SyncReservations();
            changed |= SyncVehicles();
            changed |= SyncChargingRequests();
            changed |= SyncIncidents();
            return changed;
        }

        private bool SyncHubs()
        {
            var changed = false;
            foreach (var hub in Store.Hubs)
            {
                var core = CoreHub(hub.Id) is { } id ? Gateway.FindHub(id) : null;
                var suspended = core?.Status == CoreDomain.HubStatus.Suspended;
                if (hub.IsSuspended == suspended) continue;

                hub.IsSuspended = suspended;
                changed = true;
                DemoApp.Notifications.Send(
                    suspended ? "Hub tạm dừng hoạt động" : "Hub hoạt động trở lại",
                    suspended
                        ? $"{hub.Name} tạm dừng theo quyết định của người vận hành. Không thể đặt xe, đỗ xe hoặc sạc tại đây."
                        : $"{hub.Name} đã mở lại, bạn có thể đặt xe và đỗ xe như bình thường.",
                    suspended ? Tone.Danger : Tone.Success);
            }
            return changed;
        }

        private bool SyncReservations()
        {
            var changed = false;
            foreach (var r in Store.VehicleReservations.Where(r => r.State == ReservationState.Active).ToList())
            {
                var core = Gateway.FindReservation(r.Id);
                if (core is null || core.Status is not (CoreDomain.ReservationStatus.Cancelled or CoreDomain.ReservationStatus.Expired)) continue;

                r.State = ReservationState.Cancelled;
                r.ClosedAt = Now;
                var v = Store.GetVehicle(r.VehicleCode);
                if (v is not null && v.Status == VehicleStatus.Reserved) v.Status = VehicleStatus.Available;
                DemoApp.Notifications.Send("Đặt xe đã bị hủy",
                    $"Đặt xe {r.VehicleCode} đã bị hệ thống hủy: {core.CancelReason ?? "đặt chỗ hết hiệu lực"}. Không mất phí.",
                    Tone.Danger);
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Trạng thái xe do Operator quyết định (khoá, sửa xong, điều phối sang Hub khác) được áp vào app Sinh viên.
        /// Không đụng tới xe sinh viên đang giữ, đang đi hoặc đang sạc theo yêu cầu của chính sinh viên.
        /// </summary>
        private bool SyncVehicles()
        {
            var changed = false;
            var busyCodes = Store.VehicleReservations
                .Where(r => r.State is ReservationState.Active or ReservationState.Started)
                .Select(r => r.VehicleCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var v in Store.Vehicles)
            {
                if (busyCodes.Contains(v.Code) || v.Status is VehicleStatus.InUse or VehicleStatus.Charging) continue;

                var core = Gateway.FindVehicle(v.Code);
                if (core is null) continue;

                var status = StudentStatus(core.Status, v.Status);
                if (status != v.Status)
                {
                    v.Status = status;
                    changed = true;
                }

                var hubId = StudentHub(core.HubId);
                if (core.HubId is not null && hubId is not null && hubId != v.HubId)
                {
                    // Operator điều phối xe sang Hub khác: chỗ đỗ ở Hub cũ được trả, Hub mới mất một chỗ.
                    if (v.HubId is not null)
                    {
                        var from = Store.GetHub(v.HubId);
                        from.FreeSlots = Math.Min(from.ParkingCapacity, from.FreeSlots + 1);
                    }
                    var to = Store.GetHub(hubId);
                    to.FreeSlots = Math.Max(0, to.FreeSlots - 1);
                    v.HubId = hubId;
                    v.Slot = core.Slot;
                    v.LastUpdate = Now;
                    changed = true;
                }
            }
            return changed;
        }

        private bool SyncChargingRequests()
        {
            var changed = false;
            foreach (var r in Store.ChargingRequests.Where(r => r.State == ChargingRequestState.InQueue).ToList())
            {
                var port = Gateway.FindChargingRequest(r.Id)?.AssignedPortId;
                if (port is null || !TryParsePointNumber(port, out var number)) continue;

                if (DemoApp.Charging.AssignChargingPoint(r, number) is not null) changed = true;
            }
            return changed;
        }

        private bool SyncIncidents()
        {
            var changed = false;
            foreach (var incident in Store.Incidents)
            {
                if (!_incidentIds.TryGetValue(incident.Code, out var coreId)) continue;
                var core = Demo.Store.FindIncident(coreId);
                if (core is null) continue;

                var status = core.Status switch
                {
                    CoreDomain.IncidentStatus.New => IncidentStatus.New,
                    CoreDomain.IncidentStatus.UnderCheck => IncidentStatus.UnderCheck,
                    CoreDomain.IncidentStatus.Locked => IncidentStatus.Locked,
                    CoreDomain.IncidentStatus.InRepair => IncidentStatus.InRepair,
                    CoreDomain.IncidentStatus.Resolved => IncidentStatus.Resolved,
                    _ => IncidentStatus.Closed
                };
                if (status == incident.Status) continue;

                incident.Status = status;
                changed = true;
                if (status is IncidentStatus.Resolved or IncidentStatus.Closed)
                {
                    DemoApp.Notifications.Send("Sự cố đã được xử lý",
                        $"Báo cáo {incident.Code} ({incident.ObjectCode}) đã được người vận hành xử lý xong. Cảm ơn bạn đã báo lỗi.",
                        Tone.Success);
                }
            }
            return changed;
        }

        // ---------- Đổi mã ----------

        public static string? CoreHub(string? studentHubId) =>
            studentHubId is not null && HubMap.TryGetValue(studentHubId, out var id) ? id : null;

        public static string? StudentHub(string? coreHubId) =>
            coreHubId is null ? null : HubMap.FirstOrDefault(p => p.Value == coreHubId).Key;

        /// <summary>Mã cổng của Core: CS-{số Hub}-{số cổng}, ví dụ HUB-01 cổng 3 → CS-01-03.</summary>
        public static string CorePointId(string coreHubId, int number) => $"CS-{coreHubId[4..]}-{number:00}";

        public static bool TryParsePointNumber(string portId, out int number)
        {
            number = 0;
            var dash = portId.LastIndexOf('-');
            return dash >= 0 && int.TryParse(portId[(dash + 1)..], out number);
        }

        private static CoreDomain.VehicleType MapType(VehicleType type) =>
            type == VehicleType.EBike ? CoreDomain.VehicleType.EBike : CoreDomain.VehicleType.EMotorbike;

        public static CoreDomain.DeviceStatus MapStatus(VehicleStatus status) => status switch
        {
            VehicleStatus.Available => CoreDomain.DeviceStatus.Available,
            VehicleStatus.Reserved => CoreDomain.DeviceStatus.Reserved,
            VehicleStatus.InUse => CoreDomain.DeviceStatus.InUse,
            VehicleStatus.NeedsCharge => CoreDomain.DeviceStatus.NeedsCharge,
            VehicleStatus.Charging => CoreDomain.DeviceStatus.Charging,
            VehicleStatus.Relocating => CoreDomain.DeviceStatus.Relocating,
            VehicleStatus.UnderCheck => CoreDomain.DeviceStatus.UnderCheck,
            _ => CoreDomain.DeviceStatus.Faulty
        };

        /// <summary>Trạng thái hiển thị cho sinh viên theo trạng thái Core; trạng thái không liên quan thì giữ nguyên.</summary>
        public static VehicleStatus StudentStatus(CoreDomain.DeviceStatus core, VehicleStatus current) => core switch
        {
            CoreDomain.DeviceStatus.Available => current is VehicleStatus.NeedsCharge ? current : VehicleStatus.Available,
            CoreDomain.DeviceStatus.NeedsCharge => VehicleStatus.NeedsCharge,
            CoreDomain.DeviceStatus.Reserved => VehicleStatus.Reserved,
            CoreDomain.DeviceStatus.UnderCheck => VehicleStatus.UnderCheck,
            CoreDomain.DeviceStatus.Locked or CoreDomain.DeviceStatus.UnderRepair or CoreDomain.DeviceStatus.Faulty => VehicleStatus.Faulty,
            CoreDomain.DeviceStatus.Relocating => VehicleStatus.Relocating,
            _ => current
        };

        private void Try(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Demo.Logger.Warn($"Liên thông app Sinh viên: {ex.Message}", "LINK");
            }
        }
    }
}
