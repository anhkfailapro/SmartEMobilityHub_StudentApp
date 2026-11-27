using System.Windows.Controls;
using System.Windows.Threading;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Screens;
using SmartEMobilityHub.Services;
using SmartEMobilityHub.Ui;

namespace SmartEMobilityHub
{
    public static class DemoApp
    {
        /// <summary>Liên thông với Trung tâm điều hành: dùng chung mạng lưới Core (Hub, xe, đặt chỗ, sự cố, hàng chờ sạc).</summary>
        public static NetworkLink Link { get; } = new();

        /// <summary>
        /// Dữ liệu và service của app Sinh viên (DC-51) trên mạng lưới dùng chung với Trung tâm điều hành (DC-54).
        /// Các thuộc tính dưới đây chỉ là lối tắt cho màn hình.
        /// </summary>
        public static StudentContext Context { get; } = new();

        public static SimulationClock Clock => Context.Clock;
        public static DemoToggles Toggles => Context.Toggles;
        public static MockDataStore Store => Context.Store;
        public static NotificationService Notifications => Context.Notifications;
        public static JourneyPlanner Journey => Context.Journey;
        public static VehicleManager Vehicles => Context.Vehicles;
        public static ReservationService Reservations => Context.Reservations;
        public static ParkingService Parking => Context.Parking;
        public static PickUpService PickUp => Context.PickUp;
        public static TripBillingService Billing => Context.Billing;
        public static ChargingService Charging => Context.Charging;
        public static BookingService Booking => Context.Booking;
        public static IncidentService Incidents => Context.Incidents;
        public static HistoryService History => Context.History;
        public static AppShell Shell { get; private set; } = null!;

        /// <summary>Đăng nhập dùng chung với Core; tài khoản demo nạp từ <see cref="AccountSeed"/>.</summary>
        public static AuthService Auth { get; } = CreateAuth();

        private static DemoPanel? _panel;
        private static DispatcherTimer? _timer;

        /// <param name="requireLogin">
        /// false khi chạy chế độ chụp màn hình: tự đăng nhập tài khoản demo để kịch bản vào thẳng Trang chủ.
        /// </param>
        public static void Start(Border phoneHost, Border panelHost, bool requireLogin = true)
        {
            Context.Reset();
            Link.Reset();
            Shell = new AppShell(phoneHost);
            _panel = new DemoPanel(panelHost);
            _panel.Build();
            if (requireLogin)
            {
                Shell.ResetTo(new LoginScreen());
            }
            else
            {
                var demo = Auth.Login(AccountSeed.StudentUsername, AccountSeed.StudentPassword, Core.Domain.AccountRole.Student);
                OnLoggedIn(demo.Account!);
            }
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => Tick();
            _timer.Start();
        }

        public static void OnLoggedIn(Core.Domain.UserAccount account)
        {
            Store.StudentName = account.DisplayName;
            Store.StudentId = account.LinkedId;
            Shell.ResetTo(new HomeScreen());
        }

        public static void Logout()
        {
            Auth.Logout();
            Shell.ResetTo(new LoginScreen());
        }

        private static AuthService CreateAuth()
        {
            var auth = new AuthService(Clock);
            AccountSeed.Apply(auth);
            return auth;
        }

        public static void GoTab(NavTab tab)
        {
            if (!Auth.IsLoggedIn)
            {
                Shell.ResetTo(new LoginScreen());
                return;
            }

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
            var parkingDone = Parking.CompleteFinished();
            var networkChanged = Context.Watcher.Poll();
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
            if (Shell.Current != null && (Shell.Current.WantsTick || expired.Count > 0 || parkingDone > 0 || networkChanged)) Shell.Refresh();
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
            Context.Reset();
            Link.Reset();
            Shell.ResetTo(Auth.IsLoggedIn ? new HomeScreen() : new LoginScreen());
            _panel?.Build();
        }
    }
}
