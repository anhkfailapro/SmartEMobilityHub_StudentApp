using System.Reflection;
using System.Windows.Controls;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Operator;
using SmartEMobilityHub.Operator.ViewModels;
using SmartEMobilityHub.Screens;
using SmartEMobilityHub.Tests.Ui.Support;
using SmartEMobilityHub.Ui;

namespace SmartEMobilityHub.Tests.Ui;

/// <summary>
/// App Sinh viên: màn hình nhận DemoApp qua constructor (không còn trạng thái toàn cục), app không tham chiếu
/// project Operator, và mọi màn hình dựng được trên dữ liệu thật của mạng lưới. UI-SV-01..07.
/// </summary>
public static class StudentUiTests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("UI-SV-01", "Khởi động không cần cửa sổ: đăng nhập demo, vào Trang chủ; mỗi tab mở đúng màn hình", () =>
        {
            var h = new StudentHarness();
            Assert.True(h.App.Auth.IsLoggedIn, "Đã đăng nhập tài khoản demo");
            Assert.True(h.App.Shell.Current is HomeScreen, "Vào Trang chủ");
            ScreenCoverage.Rendered.Add(typeof(HomeScreen));

            var expected = new (NavTab Tab, Type Screen)[]
            {
                (NavTab.Home, typeof(HomeScreen)), (NavTab.Booking, typeof(MyReservationsScreen)),
                (NavTab.Trip, typeof(TripScreen)), (NavTab.Notify, typeof(ActivityScreen)), (NavTab.Account, typeof(AccountScreen))
            };
            foreach (var (tab, type) in expected)
            {
                h.App.GoTab(tab);
                Assert.Equal(type, h.App.Shell.Current?.GetType(), $"Tab {tab}");
                ScreenCoverage.Rendered.Add(type);
            }

            h.App.Logout();
            Assert.True(h.App.Shell.Current is LoginScreen, "Đăng xuất về màn hình đăng nhập");
            ScreenCoverage.Rendered.Add(typeof(LoginScreen));
        });

        runner.Add("UI-SV-02", "Hai app không dính nhau, không còn trạng thái toàn cục", () =>
        {
            var references = typeof(DemoApp).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();
            Assert.False(references.Contains("SmartEMobilityHub.Operator"), "App Sinh viên không tham chiếu project Operator");
            Assert.Equal(0, typeof(DemoApp).GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Length,
                "DemoApp không còn thành viên static");

            var a = new StudentHarness();
            var b = new StudentHarness();
            a.App.Reservations.ReserveVehicle("EB-1024");
            Assert.True(a.App.Store.GetVehicle("EB-1024")!.Status == VehicleStatus.Reserved, "App A đặt được xe");
            Assert.True(b.App.Store.GetVehicle("EB-1024")!.Status == VehicleStatus.Available, "App B không bị ảnh hưởng");
        });

        runner.Add("UI-SV-03", "Demo panel: chạy riêng thì nút mở Operator tắt; có Launcher thì mở và đổi phiên khi đặt lại demo", () =>
        {
            var alone = new StudentHarness();
            Assert.False(alone.App.OpenOperatorConsole(), "Chạy riêng: không mở được Operator");
            Assert.False(OperatorButton(alone.Panel).IsEnabled, "Chạy riêng: nút bị tắt");

            var console = new FakeOperatorConsole();
            var linked = new StudentHarness(console);
            Assert.True(OperatorButton(linked.Panel).IsEnabled, "Có Launcher: nút bật");
            Assert.True(linked.App.OpenOperatorConsole(), "Mở được Operator");
            Assert.True(ReferenceEquals(linked.App.Context.Network, console.Opened.Single()), "Operator mở trên chính phiên của app");

            var before = linked.App.Context.Network;
            linked.App.ResetDemo();
            Assert.False(ReferenceEquals(before, linked.App.Context.Network), "Đặt lại demo tạo phiên mới");
            Assert.True(ReferenceEquals(linked.App.Context.Network, console.Sessions[^1]), "Operator được chuyển sang phiên mới");
        });

        runner.Add("UI-SV-04", "Liên thông: sinh viên đặt xe thì trang Xe & cổng sạc của Operator thấy ngay trên cùng phiên", () =>
        {
            var console = new FakeOperatorConsole();
            var h = new StudentHarness(console);
            h.App.OpenOperatorConsole();
            var operatorVm = new MainViewModel(new OperatorDemo(console.Opened.Single()));

            h.App.Reservations.ReserveVehicle("EB-1024");
            operatorVm.Refresh();

            operatorVm.Assets.AssetSearchText = "EB-1024";
            var row = operatorVm.Assets.VehicleRows.Single(r => r.Code == "EB-1024");
            Assert.True(row.StatusText.Length > 0 && h.App.Context.Network.Store.FindVehicle("EB-1024")!.Status == Core.Domain.DeviceStatus.Reserved,
                "Operator thấy xe đã đặt");
        });

        runner.Add("UI-SV-05", "Màn hình khám phá và đặt xe dựng được: danh sách, chi tiết, xác nhận, đặt chỗ, quét QR, nhập mã, mở khoá", () =>
        {
            var h = new StudentHarness();
            var app = h.App;
            var hub = CampusMap.DefaultHubId;
            h.Show(new VehicleListScreen(app, hub));
            h.Show(new VehicleDetailScreen(app, "EB-1024"));
            h.Show(new ConfirmReservationScreen(app, "EB-1024"));
            h.Show(new JourneyResultScreen(app, CampusMap.Destinations[0].Name));

            var reservation = app.Reservations.ReserveVehicle("EB-1024").Value!;
            h.Show(new ReservationScreen(app, true));
            h.Show(new ScanQrScreen(app));
            h.Show(new ManualCodeScreen(app));
            h.Show(new CancelConfirmScreen(app, BookingKind.Vehicle, reservation.Id));
            h.Show(new UnlockingScreen(app, reservation));
            h.Show(new MyReservationsScreen(app));

            var failed = new StudentHarness();
            var r2 = failed.App.Reservations.ReserveVehicle("EB-1024").Value!;
            var incident = failed.App.PickUp.ReceiveUnlockTimeout(r2);
            failed.Show(new UnlockFailedScreen(failed.App, r2, incident));
        });

        runner.Add("UI-SV-06", "Màn hình chuyến đi dựng được: đang đi, yêu cầu sạc, hàng chờ, trả xe, Hub đầy, hoá đơn, lịch sử, báo lỗi", () =>
        {
            var h = new StudentHarness();
            var app = h.App;
            app.Reservations.ReserveVehicle("EB-1024");
            Assert.True(app.PickUp.VerifyPickUpCode("EB-1024", null).Ok, "Quét đúng xe");
            var trip = app.PickUp.ConfirmStart(app.Reservations.ActiveVehicleReservation!)!;
            h.Show(new TripScreen(app, true));
            h.Show(new ChargingRequestScreen(app, trip.VehicleCode, trip.StartHubId));

            var request = app.Charging.SubmitChargingRequest(trip.VehicleCode, trip.StartHubId, applyBusyToggle: false);
            Assert.True(request.Ok, "Gửi được yêu cầu sạc");
            h.Show(new ChargingQueuedScreen(app, request.Value!.Id));

            h.Show(new ReturnVehicleScreen(app));
            var nearest = app.Store.GetHub("HUB-02");
            h.Show(new ReturnRefusedScreen(app, "HubFull", "Hub đã hết chỗ đỗ.", nearest, null));

            h.RealClock.AdvanceMinutes(12);
            var done = app.Billing.Complete("HUB-04");
            h.Show(new TripSummaryScreen(app, done));
            var entry = app.History.Entries().First();
            h.Show(new HistoryDetailScreen(app, entry));
            h.Show(new ActivityScreen(app, true));

            h.Show(new ReportProblemScreen(app));
            var report = app.Incidents.ReceiveStudentReport(ReportObjectKind.Vehicle, "EM-2051", null, null, "Phanh kém", "Phanh trước yếu", null);
            Assert.True(report.Ok, "Gửi được báo lỗi");
            h.Show(new ReportResultScreen(app, report.Value!, false, null));
        });

        runner.Add("UI-SV-07", "Màn hình đỗ xe và sạc xe cá nhân dựng được; mọi lớp Screen của app đều đã được dựng", () =>
        {
            var h = new StudentHarness();
            var app = h.App;
            var hub = CampusMap.DefaultHubId;

            var parkingForm = h.Show(new ReserveParkingScreen(app));
            h.Show(new ParkingFullScreen(app, parkingForm, hub, "Hub đã hết chỗ đỗ."));
            var parking = app.Parking.ReserveParking(hub, "PV1", app.Clock.Now.AddHours(1), 120);
            Assert.True(parking.Ok, "Đặt được chỗ đỗ");
            h.Show(new ParkingDetailScreen(app, parking.Value!.Id, true));
            h.Show(new ChangeTimeScreen(app, BookingKind.Parking, parking.Value.Id));

            var chargingForm = h.Show(new ScheduleChargingScreen(app));
            h.Show(new ChargingAlternativesScreen(app, chargingForm, hub, app.Clock.Now.AddHours(1), "Không còn cổng sạc trong khung giờ."));
            var start = app.Booking.FindEarliestTime(hub);
            var session = app.Booking.ScheduleCharging(hub, "PV2", start, 90, 80);
            Assert.True(session.Ok, "Lên được lịch sạc");
            h.Show(new ChargingBookingDetailScreen(app, session.Value!.Id, true));
            h.Show(new ChangeTimeScreen(app, BookingKind.ChargingSession, session.Value.Id));
            h.Show(new AccountScreen(app));

            var missing = ScreenCoverage.Missing;
            Assert.True(missing.Count == 0, "Chưa dựng: " + string.Join(", ", missing.Select(t => t.Name)));
        });
    }

    /// <summary>Nút "Mở Trung tâm điều hành" của Demo panel.</summary>
    private static Button OperatorButton(Border panel)
    {
        var scroll = (ScrollViewer)panel.Child;
        var stack = (StackPanel)scroll.Content;
        return stack.Children.OfType<Button>().First(b => b.Content is TextBlock t && t.Text.StartsWith("Mở Trung tâm điều hành"));
    }
}
