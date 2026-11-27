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
    /// <summary>
    /// App Sinh viên đang chạy: dữ liệu và service (<see cref="StudentContext"/>), khung điện thoại (<see cref="AppShell"/>),
    /// Demo panel, phiên đăng nhập và nhịp 1 giây. Mỗi cửa sổ tạo một instance và truyền nó cho màn hình qua constructor,
    /// nên màn hình không đọc trạng thái toàn cục và test dựng được app trên một <see cref="StudentContext"/> riêng.
    /// </summary>
    public sealed class DemoApp
    {
        private DemoPanel? _panel;
        private DispatcherTimer? _timer;

        /// <param name="operatorConsole">
        /// Cửa sổ Trung tâm điều hành liên thông (Launcher cung cấp). null = chạy app Sinh viên riêng lẻ.
        /// </param>
        /// <param name="context">Dữ liệu và service; null = tạo mới theo giờ thật.</param>
        public DemoApp(IOperatorConsole? operatorConsole = null, StudentContext? context = null)
        {
            OperatorConsole = operatorConsole;
            Context = context ?? new StudentContext();
            Auth = new AuthService(Context.Clock);
            AccountSeed.Apply(Auth);
        }

        /// <summary>Dữ liệu và service của app Sinh viên (DC-51) trên mạng lưới dùng chung với Trung tâm điều hành (DC-54).</summary>
        public StudentContext Context { get; }

        /// <summary>Trung tâm điều hành liên thông; null khi không chạy qua Launcher.</summary>
        public IOperatorConsole? OperatorConsole { get; }

        /// <summary>Đăng nhập dùng chung với Core; tài khoản demo nạp từ <see cref="AccountSeed"/>.</summary>
        public AuthService Auth { get; }

        public AppShell Shell { get; private set; } = null!;

        // Lối tắt cho màn hình.
        public SimulationClock Clock => Context.Clock;
        public DemoToggles Toggles => Context.Toggles;
        public MockDataStore Store => Context.Store;
        public NotificationService Notifications => Context.Notifications;
        public JourneyPlanner Journey => Context.Journey;
        public VehicleManager Vehicles => Context.Vehicles;
        public ReservationService Reservations => Context.Reservations;
        public ParkingService Parking => Context.Parking;
        public PickUpService PickUp => Context.PickUp;
        public TripBillingService Billing => Context.Billing;
        public ChargingService Charging => Context.Charging;
        public BookingService Booking => Context.Booking;
        public IncidentService Incidents => Context.Incidents;
        public HistoryService History => Context.History;

        /// <param name="requireLogin">
        /// false khi chạy chế độ chụp màn hình: tự đăng nhập tài khoản demo để kịch bản vào thẳng Trang chủ.
        /// </param>
        /// <param name="startTimer">false trong test: không chạy nhịp 1 giây của giao diện.</param>
        public void Start(Border phoneHost, Border panelHost, bool requireLogin = true, bool startTimer = true)
        {
            Context.Reset();
            OperatorConsole?.SessionChanged(Context.Network);
            Shell = new AppShell(this, phoneHost);
            _panel = new DemoPanel(this, panelHost);
            _panel.Build();
            if (requireLogin)
            {
                Shell.ResetTo(new LoginScreen(this));
            }
            else
            {
                var demo = Auth.Login(AccountSeed.StudentUsername, AccountSeed.StudentPassword, Core.Domain.AccountRole.Student);
                OnLoggedIn(demo.Account!);
            }

            if (!startTimer) return;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => Tick();
            _timer.Start();
        }

        public void OnLoggedIn(Core.Domain.UserAccount account)
        {
            Store.StudentName = account.DisplayName;
            Store.StudentId = account.LinkedId;
            Shell.ResetTo(new HomeScreen(this));
        }

        public void Logout()
        {
            Auth.Logout();
            Shell.ResetTo(new LoginScreen(this));
        }

        public void GoTab(NavTab tab)
        {
            if (!Auth.IsLoggedIn)
            {
                Shell.ResetTo(new LoginScreen(this));
                return;
            }

            Screen screen = tab switch
            {
                NavTab.Booking => new MyReservationsScreen(this),
                NavTab.Trip => new TripScreen(this),
                NavTab.Notify => new ActivityScreen(this, false),
                NavTab.Account => new AccountScreen(this),
                _ => new HomeScreen(this)
            };
            Shell.ResetTo(screen);
        }

        public void Tick()
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

        public void AfterDemoAction()
        {
            Tick();
            Shell.Refresh();
            _panel?.Build();
        }

        public void ResetDemo()
        {
            Clock.Reset();
            Toggles.Reset();
            Theme.IsDark = false;
            Context.Reset();
            OperatorConsole?.SessionChanged(Context.Network);
            Shell.ResetTo(Auth.IsLoggedIn ? new HomeScreen(this) : new LoginScreen(this));
            _panel?.Build();
        }

        /// <summary>Mở Trung tâm điều hành liên thông. false khi app Sinh viên chạy riêng (không qua Launcher).</summary>
        public bool OpenOperatorConsole()
        {
            if (OperatorConsole is null) return false;
            OperatorConsole.Open(Context.Network);
            return true;
        }
    }
}
