using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartEMobilityHub.Ui
{
    public class DemoPanel
    {
        private readonly Border _host;
        private TextBlock? _clock;
        private TextBlock? _status;

        public DemoPanel(Border host)
        {
            _host = host;
        }

        public void Build()
        {
            var sp = new StackPanel();
            sp.Children.Add(Heading("Demo panel"));
            sp.Children.Add(Note("Không thuộc ứng dụng. Dùng để giả lập GPS, IoT, thời gian và các luồng thay thế."));

            sp.Children.Add(Heading("Liên thông Trung tâm điều hành"));
            sp.Children.Add(Note("Mở bảng điều khiển của người vận hành dùng chung dữ liệu với app này: đặt / nhận / trả xe, báo lỗi, yêu cầu sạc hiện ngay bên Operator; Operator tạm dừng Hub, khoá xe, điều phối xe thì app Sinh viên cập nhật theo. Tài khoản Operator demo: op01 / op123456."));
            sp.Children.Add(Btn("Mở Trung tâm điều hành (dùng chung dữ liệu)", () =>
            {
                DemoApp.Link.OpenOperatorDashboard();
                SetStatus("Đăng nhập Operator để mở bảng điều khiển liên thông.");
            }, true));

            sp.Children.Add(Heading("Thời gian demo"));
            _clock = Note("");
            sp.Children.Add(_clock);
            var times = new WrapPanel();
            times.Children.Add(Btn("+1 phút", () => DemoApp.Clock.FastForward(1)));
            times.Children.Add(Btn("+5 phút", () => DemoApp.Clock.FastForward(5)));
            times.Children.Add(Btn("+15 phút", () => DemoApp.Clock.FastForward(15)));
            times.Children.Add(Btn("+30 phút", () => DemoApp.Clock.FastForward(30)));
            times.Children.Add(Btn("Giờ thực", () => DemoApp.Clock.Reset()));
            sp.Children.Add(times);

            var t = DemoApp.Toggles;
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
                DemoApp.Vehicles.MakeStale("EB-1024");
                SetStatus("EB-1024: cập nhật lần cuối 14 phút trước.");
            }, true));
            sp.Children.Add(Btn("UC-08 b6 · Cấp cổng sạc cho yêu cầu đầu hàng chờ", () =>
            {
                var r = DemoApp.Charging.AssignNextChargingPoint();
                SetStatus(r == null ? "Không có yêu cầu sạc nào trong hàng chờ." : $"Đã cấp cổng #{r.AssignedPoint} cho {r.VehicleCode}.");
            }, true));
            sp.Children.Add(Btn("UC-09 5a · Cổng sạc đã đặt bị hỏng (còn cổng khác)", () =>
            {
                var r = DemoApp.Booking.ChargingPointOutOfService(true);
                SetStatus(r == null ? "Không có lịch sạc đang chờ." : r == "Moved" ? "Lịch sạc đã chuyển sang cổng khác." : "Lịch sạc đã bị hủy.");
            }, true));
            sp.Children.Add(Btn("UC-09 5a · Cổng sạc đã đặt bị hỏng (hết cổng)", () =>
            {
                var r = DemoApp.Booking.ChargingPointOutOfService(false);
                SetStatus(r == null ? "Không có lịch sạc đang chờ." : "Lịch sạc đã bị hủy.");
            }, true));
            sp.Children.Add(Btn("Đặt lại toàn bộ demo", () =>
            {
                DemoApp.ResetDemo();
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
            if (_clock != null) _clock.Text = $"Giờ demo: {DemoApp.Clock.Now:dd/MM/yyyy HH:mm:ss}";
        }

        private void SetStatus(string text)
        {
            if (_status != null) _status.Text = text;
        }

        private static TextBlock Heading(string text) => new()
        {
            Text = text,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 14, 0, 6)
        };

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
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 6),
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
            DemoApp.AfterDemoAction();
            SetStatus(status);
        }
    }
}
