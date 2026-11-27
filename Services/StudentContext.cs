using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Data;

namespace SmartEMobilityHub.Services
{
    /// <summary>
    /// Toàn bộ dữ liệu và service của app Sinh viên trong một đối tượng (DC-51), gắn với một phiên mạng lưới
    /// <see cref="NetworkSession"/> dùng chung với Trung tâm điều hành (DC-54).
    /// <c>DemoApp</c> giữ một context cho UI; test tạo context mới với đồng hồ điều khiển được.
    /// </summary>
    public sealed class StudentContext
    {
        public StudentContext(IClock? realClock = null)
        {
            Clock = new SimulationClock(realClock);
            Notifications = new NotificationService(this);
            Journey = new JourneyPlanner(this);
            Vehicles = new VehicleManager(this);
            Reservations = new ReservationService(this);
            Parking = new ParkingService(this);
            PickUp = new PickUpService(this);
            Billing = new TripBillingService(this);
            Charging = new ChargingService(this);
            Booking = new BookingService(this);
            Incidents = new IncidentService(this);
            History = new HistoryService(this);
            Watcher = new NetworkWatcher(this);
            Reset();
        }

        /// <summary>Đồng hồ demo dùng chung với mạng lưới: giờ thật + phần đã tua ở Demo panel.</summary>
        public SimulationClock Clock { get; }
        public DemoToggles Toggles { get; } = new();
        public MockDataStore Store { get; } = new();

        /// <summary>Phiên mạng lưới hiện tại (đổi sau mỗi lần đặt lại demo).</summary>
        public NetworkSession Network => Store.Network;
        public StudentAppGateway Gateway => Network.StudentGateway;

        public NotificationService Notifications { get; }
        public JourneyPlanner Journey { get; }
        public VehicleManager Vehicles { get; }
        public ReservationService Reservations { get; }
        public ParkingService Parking { get; }
        public PickUpService PickUp { get; }
        public TripBillingService Billing { get; }
        public ChargingService Charging { get; }
        public BookingService Booking { get; }
        public IncidentService Incidents { get; }
        public HistoryService History { get; }

        /// <summary>Phát hiện thay đổi do Trung tâm điều hành tạo ra để báo cho sinh viên và vẽ lại màn hình.</summary>
        public NetworkWatcher Watcher { get; }

        /// <summary>Tạo mạng lưới mới (seed Core + đội xe của app) và nạp lại dữ liệu riêng của app.</summary>
        public void Reset()
        {
            Store.Seed(new NetworkSession(Clock));
            Watcher.Reset();
        }
    }
}
