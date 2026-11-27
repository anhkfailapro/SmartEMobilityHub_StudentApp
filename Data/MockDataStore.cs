using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Models;
using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Data
{
    /// <summary>
    /// Dữ liệu của app Sinh viên (DC-54).
    /// - Hub, xe, cổng sạc, đặt xe, yêu cầu sạc, sự cố: KHÔNG lưu ở đây. Đây chỉ là các "view" đọc thẳng
    ///   <see cref="Network"/> — mạng lưới chung mà Trung tâm điều hành cũng đang dùng. Không còn bước đồng bộ.
    /// - Dữ liệu chỉ app Sinh viên có (chuyến đi, hoá đơn, xe cá nhân, đặt chỗ đỗ, lịch sạc xe cá nhân,
    ///   thông báo, gói tháng) vẫn nằm trong store này.
    /// View được tạo một lần cho mỗi đối tượng Core, nên so sánh tham chiếu giữa các lần vẽ màn hình vẫn đúng.
    /// </summary>
    public class MockDataStore
    {
        private readonly Dictionary<CoreDomain.MobilityHub, Hub> _hubs = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<CoreDomain.Vehicle, Vehicle> _vehicles = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<CoreDomain.Reservation, VehicleReservation> _reservations = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<CoreDomain.IncidentRecord, Incident> _incidents = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<string> _ownIncidentIds = new(StringComparer.Ordinal);
        private NetworkSession? _network;

        /// <summary>Tên sinh viên đang đăng nhập (cập nhật khi đăng nhập).</summary>
        public string StudentName { get; set; } = "Minh";

        /// <summary>Mã sinh viên = UserId trên mạng lưới (đặt xe, báo lỗi). Mặc định là tài khoản demo.</summary>
        public string StudentId { get; set; } = AccountSeed.StudentUsername;

        public int PlanFreeMinutes { get; set; } = 1;
        public List<PrivateVehicle> PrivateVehicles { get; } = new();
        public List<ParkingReservation> ParkingReservations { get; } = new();
        public List<PrivateEVChargingSession> ChargingSessions { get; } = new();
        public List<Trip> Trips { get; } = new();
        public List<AppNotification> Notifications { get; } = new();
        public Trip? ActiveTrip { get; set; }

        /// <summary>Yêu cầu sạc sinh viên đã gửi (kể cả đã huỷ — Core xoá yêu cầu khi huỷ).</summary>
        public List<ChargingRequest> ChargingRequests { get; } = new();

        private int _counter = 100;
        private int _invoiceCounter = 43;

        /// <summary>Mạng lưới chung. Có sau lần <see cref="Seed"/> đầu tiên.</summary>
        public NetworkSession Network => _network ?? throw new InvalidOperationException("Chưa nạp mạng lưới (gọi Seed).");

        // ---------- View trên mạng lưới ----------

        public IReadOnlyList<Hub> Hubs => Network.Store.Hubs.Select(View).ToList();

        public IReadOnlyList<Vehicle> Vehicles => Network.Store.Vehicles.Select(View).ToList();

        /// <summary>Đặt xe của sinh viên đang đăng nhập (đặt chỗ của người khác không hiện).</summary>
        public IReadOnlyList<VehicleReservation> VehicleReservations =>
            Network.Store.Reservations.Where(r => r.UserId == StudentId && _reservations.ContainsKey(r))
                .Select(r => _reservations[r]).ToList();

        /// <summary>Sự cố sinh viên đã báo hoặc liên quan tới đặt xe của sinh viên.</summary>
        public IReadOnlyList<Incident> Incidents =>
            Network.Store.Incidents
                .Where(i => i.ReporterIds.Contains(StudentId) || _ownIncidentIds.Contains(i.IncidentId))
                .Select(View).ToList();

        public Hub GetHub(string id) => FindHub(id) ?? throw new KeyNotFoundException($"Không tìm thấy Hub {id}.");

        public Hub? FindHub(string? id) => id is null ? null : Network.Store.FindHub(id) is { } core ? View(core) : null;

        public Vehicle? GetVehicle(string code) => Network.Store.FindVehicle(code) is { } core ? View(core) : null;

        public PrivateVehicle GetPrivateVehicle(string id) => PrivateVehicles.First(p => p.Id == id);

        public Hub View(CoreDomain.MobilityHub core)
        {
            if (!_hubs.TryGetValue(core, out var hub))
            {
                hub = new Hub(core, QueueLength);
                _hubs[core] = hub;
            }
            return hub;
        }

        public Vehicle View(CoreDomain.Vehicle core)
        {
            if (!_vehicles.TryGetValue(core, out var vehicle))
            {
                vehicle = new Vehicle(core);
                _vehicles[core] = vehicle;
            }
            return vehicle;
        }

        public Incident View(CoreDomain.IncidentRecord core)
        {
            if (!_incidents.TryGetValue(core, out var incident))
            {
                incident = new Incident(core);
                _incidents[core] = incident;
            }
            return incident;
        }

        /// <summary>Gắn phần riêng của app (vị trí đỗ, mã nhận xe) cho một đặt xe vừa tạo trên mạng lưới.</summary>
        public VehicleReservation Track(CoreDomain.Reservation core, string slot, string pickUpCode)
        {
            var reservation = new VehicleReservation(core, slot, pickUpCode);
            _reservations[core] = reservation;
            return reservation;
        }

        public ChargingRequest Track(CoreDomain.ChargingRequest core, DateTime createdAt)
        {
            var request = new ChargingRequest(core, createdAt,
                r => Network.Store.ChargingRequests.Contains(r),
                id => Network.StudentGateway.QueuePositionOf(id));
            ChargingRequests.Add(request);
            return request;
        }

        /// <summary>Sự cố tạo cho sinh viên mà không có tên sinh viên trong danh sách người báo (lỗi mở khoá).</summary>
        public Incident TrackIncident(CoreDomain.IncidentRecord core)
        {
            _ownIncidentIds.Add(core.IncidentId);
            return View(core);
        }

        private int QueueLength(string hubId) =>
            Network.Store.ChargingRequests.Count(r => r.HubId == hubId && r.AssignedPortId is null);

        public string NextId(string prefix) => $"{prefix}-{++_counter}";

        public string NextInvoiceNumber() => $"HD-{_invoiceCounter++:0000}";

        /// <summary>
        /// Nạp lại dữ liệu riêng của app Sinh viên và gắn vào một mạng lưới mới
        /// (Hub, xe và sự cố mẫu do Core seed — <see cref="CampusFleetSeed"/>).
        /// </summary>
        public void Seed(NetworkSession network)
        {
            _network = network;
            var now = network.Clock.Now;
            _hubs.Clear(); _vehicles.Clear(); _reservations.Clear(); _incidents.Clear(); _ownIncidentIds.Clear();
            PrivateVehicles.Clear(); ParkingReservations.Clear(); ChargingSessions.Clear(); ChargingRequests.Clear();
            Trips.Clear(); Notifications.Clear();
            ActiveTrip = null;
            PlanFreeMinutes = 1;
            _counter = 100;
            _invoiceCounter = 43;

            PrivateVehicles.Add(new PrivateVehicle { Id = "PV1", Plate = "59-X1 234.56", Model = "VinFast Feliz S" });
            PrivateVehicles.Add(new PrivateVehicle { Id = "PV2", Plate = "59-X2 789.01", Model = "Yadea Odora" });

            var t1 = new Trip { Id = "T-90", VehicleCode = "EB-1024", StartHubId = "HUB-01", StartTime = now.AddHours(-2).AddMinutes(-19), StartBattery = 90, EndTime = now.AddHours(-2), EndHubId = "HUB-02", EndSlot = "C-13", DistanceKm = 3.2 };
            t1.Invoice = new Invoice { Number = "HD-0042", Minutes = 19, RentalFee = 28500, PlanDeduction = 1500, Total = 27000, PaymentMethod = DemoConfig.PaymentMethod };
            var t2 = new Trip { Id = "T-80", VehicleCode = "EM-2101", StartHubId = "HUB-04", StartTime = now.AddDays(-40).AddMinutes(-12), StartBattery = 80, EndTime = now.AddDays(-40), EndHubId = "HUB-01", EndSlot = "A-15", DistanceKm = 2.1 };
            t2.Invoice = new Invoice { Number = "HD-0031", Minutes = 12, RentalFee = 18000, Total = 18000, PaymentMethod = DemoConfig.PaymentMethod };
            var t3 = new Trip { Id = "T-50", VehicleCode = "EB-2001", StartHubId = "HUB-04", StartTime = now.AddDays(-120).AddMinutes(-8), StartBattery = 70, EndTime = now.AddDays(-120), EndHubId = "HUB-04", EndSlot = "E-15", DistanceKm = 1.0 };
            t3.Invoice = new Invoice { Number = "HD-0009", Minutes = 8, RentalFee = 12000, Total = 12000, PaymentMethod = DemoConfig.PaymentMethod };
            Trips.AddRange(new[] { t1, t2, t3 });

            ParkingReservations.Add(new ParkingReservation { Id = "PR-90", PrivateVehicleId = "PV1", HubId = "HUB-04", SlotNumber = "P-03", Start = now.AddDays(-10), DurationMinutes = 120, EntryCode = "PK-90", State = ReservationState.Cancelled, ClosedAt = now.AddDays(-10).AddHours(-1) });

            var oldReport = network.Store.Incidents.FirstOrDefault(i => i.ObjectCode == "EB-1031" && i.ReporterIds.Contains(AccountSeed.StudentUsername));
            if (oldReport is not null)
                Notifications.Add(new AppNotification { Title = "Đã tiếp nhận báo lỗi", Body = $"Mã báo cáo {oldReport.IncidentId} · EB-1031 · Người điều hành đang xử lý.", Time = now.AddDays(-1), Tone = Tone.Info, Read = true });
            Notifications.Add(new AppNotification { Title = "Đặt chỗ đã bị hủy", Body = "Hết thời gian giữ xe EM-2060. Xe đã trở về trạng thái sẵn sàng.", Time = now.AddDays(-1).AddHours(1), Tone = Tone.Danger, Read = true });
            Notifications.Add(new AppNotification { Title = "Đã có cổng sạc", Body = "Cổng sạc #4 tại Thư viện Trung tâm đã sẵn sàng cho EM-2051.", Time = now.AddHours(-3), Tone = Tone.Charging });
            Notifications.Add(new AppNotification { Title = "Thanh toán thành công", Body = "Hóa đơn #HD-0042 · 27.000đ · Chuyến 19 phút.", Time = now.AddHours(-2), Tone = Tone.Success });
        }
    }
}
