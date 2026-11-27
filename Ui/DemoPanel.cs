using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartEMobilityHub.Ui
{
    public class DemoPanel
    {
        private readonly DemoApp _app;
        private readonly Border _host;
        private TextBlock? _clock;
        private TextBlock? _status;
        private TextBlock? _linkHealth;

        public DemoPanel(DemoApp app, Border host)
        {
            _app = app;
            _host = host;
        }

        public void Build()
        {
            var sp = new StackPanel();
            sp.Children.Add(Heading("Demo panel"));
            sp.Children.Add(Note("Không thuộc ứng dụng. Dùng để giả lập GPS, IoT, thời gian và các luồng thay thế."));

            sp.Children.Add(Heading("Liên thông Trung tâm điều hành"));
            sp.Children.Add(Note("App Sinh viên và Trung tâm điều hành dùng chung một mạng lưới: đặt / nhận / trả xe, báo lỗi, yêu cầu sạc hiện ngay bên Operator; Operator tạm dừng Hub, khoá xe, điều phối xe thì app Sinh viên thấy ngay. Tài khoản Operator demo: op01 / op123456."));
            var open = Btn("Mở Trung tâm điều hành (dùng chung dữ liệu)", () =>
            {
                SetStatus(_app.OpenOperatorConsole()
                    ? "Đăng nhập Operator để mở bảng điều khiển liên thông."
                    : "App Sinh viên đang chạy riêng. Chạy SmartEMobilityHub.Launcher để mở cả hai app trên cùng một mạng lưới.");
            }, true);
            open.IsEnabled = _app.OperatorConsole is not null;
            if (_host.TryFindResource("DemoPrimaryButton") is Style primary) open.Style = primary;
            open.Padding = new Thickness(12, 9, 12, 9);
            sp.Children.Add(open);
            _linkHealth = Note("");
            sp.Children.Add(_linkHealth);

            sp.Children.Add(Heading("Thời gian demo"));
            _clock = Note("");
            sp.Children.Add(_clock);
            var times = new WrapPanel();
            times.Children.Add(Btn("+1 phút", () => _app.Clock.FastForward(1)));
            times.Children.Add(Btn("+5 phút", () => _app.Clock.FastForward(5)));
            times.Children.Add(Btn("+15 phút", () => _app.Clock.FastForward(15)));
            times.Children.Add(Btn("+30 phút", () => _app.Clock.FastForward(30)));
            times.Children.Add(Btn("Giờ thực", () => _app.Clock.Reset()));
            sp.Children.Add(times);

            var t = _app.Toggles;
            sp.Children.Add(Heading("Công tắc tình huống"));
            sp.Children.Add(Check("UC-01 4a · Dịch vụ bản đồ không phản hồi", t.MapServiceTimeout, v => t.MapServiceTimeout = v));
            sp.Children.Add(Check("UC-03 2a · SV khác đặt xe trước (1 lần)", t.AnotherStudentReservesFirst, v => t.AnotherStudentReservesFirst = v));
            sp.Children.Add(Check("UC-06 · Sinh viên chưa ở Hub", t.NotAtHub, v => t.NotAtHub = v));
            sp.Children.Add(Check("UC-06 4a · Xe không xác nhận mở khóa (30 giây)", t.UnlockTimeout, v => t.UnlockTimeout = v));
            sp.Children.Add(Check("UC-07 2a · Xe không nằm trong Hub", t.VehicleOutsideHub, v => t.VehicleOutsideHub = v));
            sp.Children.Add(Check("UC-07 2b · Hub trả xe hết chỗ", t.ReturnHubFull, v => t.ReturnHubFull = v));
            sp.Children.Add(Check("UC-04 4a · Hub đỗ xe đã chọn hết chỗ", t.ParkingHubFull, v => t.ParkingHubFull = v));
            sp.Children.Add(Check("UC-05 3c · Khung giờ mới không còn chỗ", t.ChangeTimeUnavailable, v => t.ChangeTimeUnavailable = v));
            sp.Children.Add(Check("UC-08 2a · Tất cả cổng sạc đang bận", t.AllChargingPointsBusy, v => t.AllChargingPointsBusy = v));
            sp.Children.Add(Check("UC-09 4a · Không còn cổng sạc trong khung giờ", t.NoChargingPointInWindow, v => t.NoChargingPointInWindow = v));
            sp.Children.Add(Check("UC-11 4a · Không có lịch sử 3 tháng", t.HistoryEmpty, v => t.HistoryEmpty = v));

            sp.Children.Add(Heading("Sự kiện hệ thống"));
            sp.Children.Add(Btn("UC-02 2a · Làm cũ dữ liệu EB-1024 (14 phút)", () =>
            {
                _app.Vehicles.MakeStale("EB-1024");
                SetStatus("EB-1024: cập nhật lần cuối 14 phút trước.");
            }, true));
            sp.Children.Add(Btn("UC-08 b6 · Cấp cổng sạc cho yêu cầu đầu hàng chờ", () =>
            {
                var r = _app.Charging.AssignNextChargingPoint();
                SetStatus(r == null ? "Không có yêu cầu sạc nào trong hàng chờ." : $"Đã cấp cổng #{r.AssignedPoint} cho {r.VehicleCode}.");
            }, true));
            sp.Children.Add(Btn("UC-09 5a · Cổng sạc đã đặt bị hỏng (còn cổng khác)", () =>
            {
                var r = _app.Booking.ChargingPointOutOfService(true);
                SetStatus(r == null ? "Không có lịch sạc đang chờ." : r == "Moved" ? "Lịch sạc đã chuyển sang cổng khác." : "Lịch sạc đã bị hủy.");
            }, true));
            sp.Children.Add(Btn("UC-09 5a · Cổng sạc đã đặt bị hỏng (hết cổng)", () =>
            {
                var r = _app.Booking.ChargingPointOutOfService(false);
                SetStatus(r == null ? "Không có lịch sạc đang chờ." : "Lịch sạc đã bị hủy.");
            }, true));
            sp.Children.Add(Btn("Đặt lại toàn bộ demo", () =>
            {
                _app.ResetDemo();
                SetStatus("Đã đặt lại dữ liệu demo.");
            }, true));

            _status = Note("");
            _status.Foreground = Brushes.DarkGreen;
            sp.Children.Add(_status);

            _host.Child = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            UpdateClock();
        }

        public void UpdateClock()
        {
            if (_clock != null) _clock.Text = $"Giờ demo: {_app.Clock.Now:dd/MM/yyyy HH:mm:ss}";
            UpdateLinkHealth();
        }

        /// <summary>App Sinh viên và Trung tâm điều hành dùng chung một mạng lưới (DC-54): hiện quy mô và trạng thái cửa sổ Operator.</summary>
        private void UpdateLinkHealth()
        {
            if (_linkHealth == null) return;
            var network = _app.Context.Network.Store;
            var window = _app.OperatorConsole is null ? "không có (chạy Launcher để liên thông)"
                : _app.OperatorConsole.IsOpen ? "đang mở" : "chưa mở";
            _linkHealth.Text = $"Mạng lưới chung: {network.Hubs.Count} Hub · {network.Vehicles.Count} xe · {network.Incidents.Count(i => i.IsOpen())} sự cố đang mở. Cửa sổ Operator: {window}.";
            _linkHealth.Foreground = Brushes.DimGray;
        }

        private void SetStatus(string text)
        {
            if (_status != null) _status.Text = text;
        }

        private static UIElement Heading(string text)
        {
            var bar = new Border { Width = 4, Height = 16, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            bar.Background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#2563EB"), (Color)ColorConverter.ConvertFromString("#7C3AED"), 90);
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 8) };
            sp.Children.Add(bar);
            sp.Children.Add(new TextBlock { Text = text, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")) });
            return sp;
        }

        private static TextBlock Note(string text) => new()
        {
            Text = text,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 6)
        };

        private Button Btn(string label, Action action, bool stretch = false)
        {
            var b = new Button
            {
                Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap },
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 6, 6),
                Style = _host.TryFindResource("DemoButton") as Style,
                HorizontalAlignment = stretch ? HorizontalAlignment.Stretch : HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            b.Click += (_, _) =>
            {
                action();
                AfterAction();
            };
            return b;
        }

        private CheckBox Check(string label, bool value, Action<bool> set)
        {
            var c = new CheckBox
            {
                Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap },
                IsChecked = value,
                Margin = new Thickness(0, 0, 0, 6)
            };
            c.Click += (_, _) =>
            {
                set(c.IsChecked == true);
                AfterAction();
            };
            return c;
        }

        private void AfterAction()
        {
            var status = _status?.Text ?? "";
            _app.AfterDemoAction();
            SetStatus(status);
        }
    }
}
