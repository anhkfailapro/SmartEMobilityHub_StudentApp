using System.Windows.Controls;
using System.Windows.Threading;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Screens;
using SmartEMobilityHub.Services;
using SmartEMobilityHub.Ui;

namespace SmartEMobilityHub
{
    public static class DemoApp
    {
        public static DemoClock Clock { get; } = new();
        public static DemoToggles Toggles { get; } = new();
        public static MockDataStore Store { get; } = new();
        public static NotificationService Notifications { get; } = new();
        public static JourneyPlanner Journey { get; } = new();
        public static VehicleManager Vehicles { get; } = new();
        public static ReservationService Reservations { get; } = new();
        public static ParkingService Parking { get; } = new();
        public static PickUpService PickUp { get; } = new();
        public static TripBillingService Billing { get; } = new();
        public static ChargingService Charging { get; } = new();
        public static BookingService Booking { get; } = new();
        public static IncidentService Incidents { get; } = new();
        public static HistoryService History { get; } = new();
        public static AppShell Shell { get; private set; } = null!;

        private static DemoPanel? _panel;
        private static DispatcherTimer? _timer;

        public static void Start(Border phoneHost, Border panelHost)
        {
            Store.Seed(Clock.Now);
            Shell = new AppShell(phoneHost);
            _panel = new DemoPanel(panelHost);
            _panel.Build();
            Shell.ResetTo(new HomeScreen());
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => Tick();
            _timer.Start();
        }

        public static void GoTab(NavTab tab)
        {
            Screen screen = tab switch
            {
                NavTab.Booking => new MyReservationsScreen(),
                NavTab.Trip => new TripScreen(),
                NavTab.Notify => new ActivityScreen(false),
                NavTab.Account => new AccountScreen(),
                _ => new HomeScreen()
            };
            Shell.ResetTo(screen);
        }

        public static void Tick()
        {
            var expired = Reservations.AutoCancelExpired();
            Booking.ProcessSessions();
            var current = Shell.Current;
            if (expired.Count > 0 && current is ReservationScreen or ScanQrScreen or ManualCodeScreen)
            {
                var r = expired[0];
                Shell.ShowDialog(Tone.Danger, "Đặt chỗ đã hết hạn",
                    $"Hết {DemoConfig.HoldMinutes} phút giữ xe. Đặt chỗ đã bị hủy và {r.VehicleCode} đã trở về trạng thái sẵn sàng.",
                    new DialogAction("Tìm xe khác", ButtonVariant.Primary, () => GoTab(NavTab.Home)),
                    new DialogAction("Đóng", ButtonVariant.Outline, () => GoTab(NavTab.Home)));
            }
            current?.OnTick();
            if (Shell.Current != null && (Shell.Current.WantsTick || expired.Count > 0)) Shell.Refresh();
            _panel?.UpdateClock();
        }

        public static void AfterDemoAction()
        {
            Tick();
            Shell.Refresh();
            _panel?.Build();
        }

        public static void ResetDemo()
        {
            Clock.Reset();
            Toggles.Reset();
            Theme.IsDark = false;
            Store.Seed(Clock.Now);
            Shell.ResetTo(new HomeScreen());
            _panel?.Build();
        }
    }
}
