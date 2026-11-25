using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Data
{
    public class MockDataStore
    {
        /// <summary>Tên sinh viên đang đăng nhập (cập nhật khi đăng nhập).</summary>
        public string StudentName { get; set; } = "Minh";
        public string StudentId { get; set; } = "";
        public int PlanFreeMinutes { get; set; } = 1;
        public List<Hub> Hubs { get; } = new();
        public List<Vehicle> Vehicles { get; } = new();
        public List<PrivateVehicle> PrivateVehicles { get; } = new();
        public List<VehicleReservation> VehicleReservations { get; } = new();
        public List<ParkingReservation> ParkingReservations { get; } = new();
        public List<PrivateEVChargingSession> ChargingSessions { get; } = new();
        public List<ChargingRequest> ChargingRequests { get; } = new();
        public List<Trip> Trips { get; } = new();
        public List<Incident> Incidents { get; } = new();
        public List<AppNotification> Notifications { get; } = new();
        public Trip? ActiveTrip { get; set; }

        private int _counter = 100;
        private int _invoiceCounter = 43;
        private int _reportCounter = 1188;

        public string NextId(string prefix) => $"{prefix}-{++_counter}";

        public string NextInvoiceNumber() => $"HD-{_invoiceCounter++:0000}";

        public string NextReportCode() => $"BC-{_reportCounter++}";

        public Hub GetHub(string id) => Hubs.First(h => h.Id == id);

        public Vehicle? GetVehicle(string code) => Vehicles.FirstOrDefault(v => string.Equals(v.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

        public PrivateVehicle GetPrivateVehicle(string id) => PrivateVehicles.First(p => p.Id == id);

        public void Seed(DateTime now)
        {
            Hubs.Clear(); Vehicles.Clear(); PrivateVehicles.Clear(); VehicleReservations.Clear(); ParkingReservations.Clear();
            ChargingSessions.Clear(); ChargingRequests.Clear(); Trips.Clear(); Incidents.Clear(); Notifications.Clear();
            ActiveTrip = null;
            PlanFreeMinutes = 1;

            var h1 = new Hub { Id = "H1", Name = "Hub Ga Metro Suối Tiên", ShortName = "Ga Metro Suối Tiên", X = 0.35, Y = 0, ParkingCapacity = 20, FreeSlots = 3, MaxChargingMinutes = 180, QueueLength = 2 };
            h1.Points.AddRange(new[] { P(1, PointStatus.Free), P(2, PointStatus.Free), P(3, PointStatus.Charging), P(4, PointStatus.Charging), P(5, PointStatus.Charging), P(6, PointStatus.OutOfService) });
            var h2 = new Hub { Id = "H2", Name = "Hub Thư viện Trung tâm", ShortName = "Thư viện Trung tâm", X = 0, Y = 0.8, ParkingCapacity = 15, FreeSlots = 5, MaxChargingMinutes = 120, QueueLength = 1 };
            h2.Points.AddRange(new[] { P(1, PointStatus.Free), P(2, PointStatus.Charging), P(3, PointStatus.Charging), P(4, PointStatus.Free) });
            var h3 = new Hub { Id = "H3", Name = "Hub KTX Khu A", ShortName = "KTX Khu A", X = -1.2, Y = 0, ParkingCapacity = 12, FreeSlots = 0, MaxChargingMinutes = 240, QueueLength = 0 };
            h3.Points.AddRange(new[] { P(1, PointStatus.Charging), P(2, PointStatus.Free) });
            var h4 = new Hub { Id = "H4", Name = "Hub Nhà văn hóa SV", ShortName = "Nhà văn hóa SV", X = 0.3, Y = 1.5, ParkingCapacity = 10, FreeSlots = 6, MaxChargingMinutes = 180, QueueLength = 0 };
            h4.Points.AddRange(new[] { P(1, PointStatus.Free), P(2, PointStatus.Free) });
            Hubs.AddRange(new[] { h1, h2, h3, h4 });

            V("EB-1024", VehicleType.EBike, "H1", "A-07", 82, VehicleStatus.Available, now.AddMinutes(-2));
            V("EM-2051", VehicleType.EMotorbike, "H1", "A-02", 58, VehicleStatus.Available, now.AddMinutes(-2));
            V("EB-1031", VehicleType.EBike, "H1", "A-04", 47, VehicleStatus.Reserved, now.AddMinutes(-3));
            V("EM-2060", VehicleType.EMotorbike, "H1", "A-09", 15, VehicleStatus.Faulty, now.AddMinutes(-14));
            V("EB-1040", VehicleType.EBike, "H1", "A-11", 91, VehicleStatus.Available, now.AddMinutes(-1));
            V("EM-2072", VehicleType.EMotorbike, "H1", "A-12", 64, VehicleStatus.Available, now.AddMinutes(-4));
            V("EB-1055", VehicleType.EBike, "H1", "A-14", 23, VehicleStatus.Available, now.AddMinutes(-5));
            V("EB-2001", VehicleType.EBike, "H2", "C-01", 76, VehicleStatus.Available, now.AddMinutes(-2));
            V("EM-2101", VehicleType.EMotorbike, "H2", "C-03", 88, VehicleStatus.Available, now.AddMinutes(-3));
            V("EB-2010", VehicleType.EBike, "H2", "C-05", 54, VehicleStatus.Available, now.AddMinutes(-6));
            V("EM-2110", VehicleType.EMotorbike, "H2", "C-06", 18, VehicleStatus.NeedsCharge, now.AddMinutes(-2));
            V("EB-3001", VehicleType.EBike, "H3", "B-01", 70, VehicleStatus.Available, now.AddMinutes(-2));
            V("EM-3101", VehicleType.EMotorbike, "H3", "B-02", 45, VehicleStatus.Available, now.AddMinutes(-7));
            V("EM-4001", VehicleType.EMotorbike, "H4", "D-01", 30, VehicleStatus.Faulty, now.AddMinutes(-3));

            PrivateVehicles.Add(new PrivateVehicle { Id = "PV1", Plate = "59-X1 234.56", Model = "VinFast Feliz S" });
            PrivateVehicles.Add(new PrivateVehicle { Id = "PV2", Plate = "59-X2 789.01", Model = "Yadea Odora" });

            Incidents.Add(new Incident { Code = "BC-1187", ObjectKind = ReportObjectKind.Vehicle, ObjectCode = "EB-1031", HubId = "H1", ProblemType = "Khóa không mở", Status = IncidentStatus.UnderCheck, Source = IncidentSource.StudentReport, CreatedAt = now.AddDays(-1) });

            var t1 = new Trip { Id = "T-90", VehicleCode = "EB-1024", StartHubId = "H1", StartTime = now.AddHours(-2).AddMinutes(-19), StartBattery = 90, EndTime = now.AddHours(-2), EndHubId = "H3", EndSlot = "B-03", DistanceKm = 3.2 };
            t1.Invoice = new Invoice { Number = "HD-0042", Minutes = 19, RentalFee = 28500, PlanDeduction = 1500, Total = 27000, PaymentMethod = DemoConfig.PaymentMethod };
            var t2 = new Trip { Id = "T-80", VehicleCode = "EM-2101", StartHubId = "H2", StartTime = now.AddDays(-40).AddMinutes(-12), StartBattery = 80, EndTime = now.AddDays(-40), EndHubId = "H1", EndSlot = "A-03", DistanceKm = 2.1 };
            t2.Invoice = new Invoice { Number = "HD-0031", Minutes = 12, RentalFee = 18000, Total = 18000, PaymentMethod = DemoConfig.PaymentMethod };
            var t3 = new Trip { Id = "T-50", VehicleCode = "EB-2001", StartHubId = "H2", StartTime = now.AddDays(-120).AddMinutes(-8), StartBattery = 70, EndTime = now.AddDays(-120), EndHubId = "H2", EndSlot = "C-02", DistanceKm = 1.0 };
            t3.Invoice = new Invoice { Number = "HD-0009", Minutes = 8, RentalFee = 12000, Total = 12000, PaymentMethod = DemoConfig.PaymentMethod };
            Trips.AddRange(new[] { t1, t2, t3 });

            ParkingReservations.Add(new ParkingReservation { Id = "PR-90", PrivateVehicleId = "PV1", HubId = "H2", SlotNumber = "P-03", Start = now.AddDays(-10), DurationMinutes = 120, EntryCode = "PK-90", State = ReservationState.Cancelled, ClosedAt = now.AddDays(-10).AddHours(-1) });

            Notifications.Add(new AppNotification { Title = "Đã tiếp nhận báo lỗi", Body = "Mã báo cáo BC-1187 · EB-1031 · Người điều hành đang xử lý.", Time = now.AddDays(-1), Tone = Tone.Info, Read = true });
            Notifications.Add(new AppNotification { Title = "Đặt chỗ đã bị hủy", Body = "Hết thời gian giữ xe EM-2060. Xe đã trở về trạng thái sẵn sàng.", Time = now.AddDays(-1).AddHours(1), Tone = Tone.Danger, Read = true });
            Notifications.Add(new AppNotification { Title = "Đã có cổng sạc", Body = "Cổng sạc #4 tại Thư viện Trung tâm đã sẵn sàng cho EM-2051.", Time = now.AddHours(-3), Tone = Tone.Charging });
            Notifications.Add(new AppNotification { Title = "Thanh toán thành công", Body = "Hóa đơn #HD-0042 · 27.000đ · Chuyến 19 phút.", Time = now.AddHours(-2), Tone = Tone.Success });
        }

        private static ChargingPoint P(int n, PointStatus s) => new() { Number = n, Status = s };

        private void V(string code, VehicleType type, string hub, string slot, int battery, VehicleStatus status, DateTime updated) =>
            Vehicles.Add(new Vehicle { Code = code, Type = type, HubId = hub, Slot = slot, Battery = battery, Status = status, LastUpdate = updated });
    }
}
