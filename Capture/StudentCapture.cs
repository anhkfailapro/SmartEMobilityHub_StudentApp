using System.Windows;
using System.Windows.Controls;
using SmartEMobilityHub.Screens;
using SmartEMobilityHub.Ui;

namespace SmartEMobilityHub.Capture
{
    /// <summary>
    /// Kịch bản chụp lần lượt các màn hình của app Sinh viên theo từng use case (UC-01 … UC-11),
    /// gồm luồng chính và các luồng thay thế/ngoại lệ tiêu biểu.
    /// Chạy: dotnet run -- --capture demo_screens\student
    /// </summary>
    internal static class StudentCapture
    {
        private static ShotKit _kit = null!;
        private static Border _phone = null!;
        private static AppShell Shell => DemoApp.Shell;

        public static async Task RunAsync(Border phoneHost, string dir)
        {
            _kit = new ShotKit(dir);
            _phone = phoneHost;
            await ShotKit.Settle(800);
            try
            {
                await Script();
            }
            catch (Exception ex)
            {
                _kit.Log("Dừng kịch bản do lỗi: " + ex);
            }
            _kit.Finish();
            Application.Current.Shutdown();
        }

        // ---------- helpers ----------
        private static async Task C(string text, bool prefix = false, int nth = 0, bool last = false)
        {
            _kit.Click(_phone, text, prefix, nth, last);
            await ShotKit.Settle();
        }

        private static bool Visible(string text) =>
            ShotKit.Flatten(_phone).OfType<System.Windows.Controls.TextBlock>().Any(t => t.IsVisible && t.Text == text);

        private static async Task T(int index, string text)
        {
            _kit.Type(_phone, index, text);
            await ShotKit.Settle(100);
        }

        private static async Task Demo(Action action)
        {
            action();
            DemoApp.AfterDemoAction();
            await ShotKit.Settle();
        }

        private static async Task Tab(NavTab tab)
        {
            DemoApp.GoTab(tab);
            await ShotKit.Settle();
        }

        private static async Task CloseDialog()
        {
            Shell.CloseDialog();
            await ShotKit.Settle();
        }

        /// <summary>Chụp khung điện thoại. full = true: kéo dài khung để thấy toàn bộ nội dung cuộn.</summary>
        private static async Task Shot(string title, string caption, bool full = true)
        {
            await ShotKit.Settle(300);
            var bezel = (FrameworkElement)_phone.Parent;
            var grid = (FrameworkElement)bezel.Parent;
            double oh = _phone.Height, bh = bezel.Height, gh = grid.Height;
            if (full)
            {
                for (var pass = 0; pass < 3; pass++)
                {
                    var sv = ShotKit.Flatten(_phone).OfType<ScrollViewer>().FirstOrDefault();
                    if (sv == null) break;
                    var extra = sv.ExtentHeight - sv.ViewportHeight;
                    if (extra <= 1) break;
                    extra = Math.Min(extra, 1600 - (_phone.Height - oh));
                    if (extra <= 1) break;
                    _phone.Height += extra;
                    bezel.Height += extra;
                    grid.Height += extra;
                    await ShotKit.Settle(150);
                }
            }
            _kit.Save(bezel, title, caption, 2);
            _phone.Height = oh;
            bezel.Height = bh;
            grid.Height = gh;
            await ShotKit.Settle(100);
        }

        private static Task Dialog(string title, string caption) => Shot(title, caption, false);

        // ---------- kịch bản ----------
        private static async Task Script()
        {
            var toggles = DemoApp.Toggles;

            // ===== UC-01 =====
            _kit.Section = "UC-01 Lập kế hoạch hành trình (Plan journey)";
            await Shot("Trang chủ",
                "Sinh viên mở ứng dụng ở màn hình Trang chủ: ô nhập điểm đến kèm gợi ý, lối tắt Đặt chỗ đỗ xe / Lên lịch sạc / Hoạt động, bản đồ mini và danh sách Hub gần nhất.");
            await T(0, "Thư viện Trung tâm");
            await C("Tìm");
            await Shot("Kết quả hành trình",
                "Sau khi nhập điểm đến “Thư viện Trung tâm” và bấm Tìm, hệ thống đề xuất Hub xuất phát gần sinh viên, Hub đến gần điểm đến, số xe sẵn sàng, chỗ trống để trả xe và thời gian di chuyển ước tính.");
            await C("Chọn Hub xuất phát");
            await Shot("Danh sách xe tại Hub xuất phát",
                "Bấm “Chọn Hub xuất phát” mở danh sách xe tại Hub: mã xe, loại xe, vị trí slot, mức pin, quãng đường còn đi được và trạng thái; có bộ lọc theo pin và loại xe.");

            await Tab(NavTab.Home);
            toggles.MapServiceTimeout = true;
            await T(0, "Thư viện Trung tâm");
            await C("Tìm");
            await Shot("Luồng thay thế 4a – Dịch vụ bản đồ không phản hồi",
                "Khi dịch vụ bản đồ không phản hồi sau 5 giây, hệ thống báo không có dữ liệu lộ trình và sắp xếp các Hub theo khoảng cách đường thẳng để sinh viên vẫn chọn được Hub.");
            toggles.MapServiceTimeout = false;

            await Tab(NavTab.Home);
            await C("Xem tất cả");
            await C("Hub Nhà văn hóa SV");
            await Shot("Luồng thay thế 7a – Hub không còn xe sẵn sàng",
                "Chọn Hub Nhà văn hóa SV (không còn xe sẵn sàng): hệ thống thông báo và gợi ý 3 Hub gần nhất còn xe.");

            // ===== UC-02 =====
            _kit.Section = "UC-02 Xem trạng thái phương tiện (View vehicle status)";
            await Tab(NavTab.Home);
            await C("Hub Ga Metro Suối Tiên");
            await C("Pin > 50%");
            await Shot("Lọc xe có pin > 50%",
                "Tại Hub Ga Metro Suối Tiên, sinh viên chọn bộ lọc “Pin > 50%”: danh sách chỉ còn các xe đủ pin cho chuyến đi.");
            await C("Tất cả");
            await C("EB-1024");
            await Shot("Chi tiết xe EB-1024",
                "Màn hình chi tiết xe: trạng thái Sẵn sàng, mức pin, quãng đường ước tính, thời gian giữ xe, giá thuê và thời điểm cập nhật dữ liệu gần nhất. Nút “Đặt trước” đang bật.");
            Shell.Back();
            await ShotKit.Settle();
            await C("EM-2060");
            await Shot("Ngoại lệ 2a/3a – Dữ liệu cũ và xe đang hỏng",
                "Xe EM-2060 có dữ liệu cập nhật cách đây 14 phút (quá ngưỡng 10 phút) nên hiện cảnh báo dữ liệu cũ; xe đang hỏng nên trang chỉ để xem và nút “Đặt trước” bị vô hiệu.");

            // ===== UC-03 =====
            _kit.Section = "UC-03 Đặt trước phương tiện (Reserve vehicle)";
            Shell.Back();
            await ShotKit.Settle();
            await C("EB-1024");
            await C("Đặt trước");
            await Shot("Xác nhận đặt xe",
                "Bấm “Đặt trước” ở xe EB-1024: màn hình xác nhận hiển thị mã xe, Hub, vị trí đỗ, thời gian giữ xe 15 phút, phí giữ xe (miễn phí) và giá thuê.");
            await C("Xác nhận đặt");
            await Shot("Đặt xe thành công – mã nhận xe",
                "Đặt xe thành công: đồng hồ đếm ngược thời gian giữ xe còn lại, mã nhận xe (pick-up code) và mã QR. Thông báo xác nhận được gửi tới sinh viên.");
            await Tab(NavTab.Home);
            await C("Hub Ga Metro Suối Tiên");
            await C("EB-1040");
            await C("Đặt trước");
            await Dialog("Ngoại lệ 2b – Đã có một đặt xe đang hoạt động",
                "Khi sinh viên thử đặt thêm xe EB-1040 trong lúc đang giữ EB-1024, hệ thống từ chối vì mỗi tài khoản chỉ có 1 đặt xe đang hoạt động, và cho mở lại đặt chỗ hiện tại.");
            await CloseDialog();

            // ===== UC-06 =====
            _kit.Section = "UC-06 Nhận xe (Pick up vehicle)";
            await Tab(NavTab.Home);
            Shell.Navigate(new ReservationScreen());
            await ShotKit.Settle();
            await C("Nhập mã thủ công");
            await Shot("Luồng thay thế 1a – Nhập mã thủ công",
                "Nếu mã QR trên xe bị hỏng, sinh viên nhập mã xe (dán trên thân xe) và mã nhận xe để xác thực.");
            Shell.Back();
            await ShotKit.Settle();
            await C("Quét QR để nhận xe");
            await Shot("Quét mã QR trên xe",
                "Màn hình quét QR. Vì máy tính không có camera, bản demo dùng nút giả lập quét đúng xe hoặc quét nhầm xe khác.");
            await C("Quét nhầm", prefix: true);
            await Dialog("Ngoại lệ 2a – Quét nhầm xe",
                "Quét nhầm mã của xe khác: hệ thống báo sai xe và cho thử lại (tối đa 2 lần thử thêm).");
            await CloseDialog();
            await C("Giả lập quét", prefix: true);
            await Shot("Đang mở khóa xe",
                "Quét đúng xe EB-1024: hệ thống đã xác thực sinh viên và mã xe, gửi lệnh mở khóa và chờ xe xác nhận (tối đa 30 giây).");
            await Task.Delay(4000);
            await ShotKit.Settle();
            await Shot("Mở khóa thành công – chuyến đi bắt đầu",
                "Xe xác nhận mở khóa, chuyến đi bắt đầu: màn hình hiển thị thời gian đã đi, mức pin, quãng đường và phí tạm tính theo thời gian thực.");

            // ===== UC-08 =====
            _kit.Section = "UC-08 Yêu cầu sạc (Charging request)";
            DemoApp.Clock.FastForward(18);
            await Tab(NavTab.Trip);
            await C("Yêu cầu sạc");
            await Shot("Yêu cầu sạc cho xe đang dùng",
                "Trong chuyến đi, sinh viên bấm “Yêu cầu sạc”: hệ thống hiển thị tình trạng các cổng sạc tại Hub, thời gian chờ ước tính, mức ưu tiên và vị trí dự kiến trong hàng chờ.");
            await C("Chọn Hub khác", prefix: true);
            await Shot("Luồng thay thế 3a – Chọn Hub khác",
                "Bấm “Chọn Hub khác” để xem các Hub khác kèm thời gian chờ, vị trí hàng chờ và số cổng trống.");
            await C("Chọn Hub khác", prefix: true);
            await C("Xác nhận yêu cầu sạc");
            await Shot("Đã vào hàng chờ sạc",
                "Gửi yêu cầu thành công: xe được đưa vào hàng chờ sạc với vị trí và thời gian chờ dự kiến. Khi có cổng trống, sinh viên nhận thông báo “Đã có cổng sạc”.");
            await Demo(() => DemoApp.Charging.AssignNextChargingPoint());

            // ===== UC-07 (phần 1) =====
            _kit.Section = "UC-07 Trả xe (Return vehicle)";
            await Tab(NavTab.Trip);
            await Shot("Chuyến đi đang diễn ra",
                "Màn hình Chuyến đi sau khoảng 18 phút: thời gian đã đi, pin, quãng đường, phí hiện tại cùng các nút Yêu cầu sạc, Báo lỗi và Trả xe.");

            // ===== UC-10 (ngoại lệ trong chuyến) =====
            _kit.Section = "UC-10 Báo lỗi (Report problem)";
            await C("Báo lỗi");
            await C("Gửi báo cáo");
            await Dialog("Luồng thay thế 2a – Báo lỗi chính xe đang sử dụng",
                "Khi báo lỗi chính xe đang đi, hệ thống yêu cầu kết thúc chuyến (trả xe) trước rồi mới gửi báo cáo.");
            await CloseDialog();

            // ===== UC-07 (tiếp) =====
            _kit.Section = "UC-07 Trả xe (Return vehicle)";
            await Tab(NavTab.Trip);
            await C("Trả xe");
            await Shot("Chọn Hub trả xe",
                "Bấm “Trả xe”: sinh viên chọn Hub đang trả xe, xem số chỗ trống, thời gian đã đi và phí tạm tính trước khi xác nhận.");
            await C("KTX Khu A");
            await C("Xác nhận trả xe");
            await Shot("Ngoại lệ 2b – Hub trả xe hết chỗ",
                "Trả xe tại KTX Khu A (hết chỗ): hệ thống từ chối, gợi ý Hub gần nhất còn chỗ và cộng thêm 10 phút miễn phí để sinh viên di chuyển tới đó.");
            await C("Đã đến", prefix: true);
            await Shot("Trả xe thành công – hóa đơn",
                "Trả xe tại Hub được gợi ý: xe được khóa, hệ thống tính phí (đã trừ phút miễn phí), thanh toán qua cổng thanh toán và gửi biên lai vào mục Thông báo.");

            // ===== UC-10 =====
            _kit.Section = "UC-10 Báo lỗi (Report problem)";
            await C("Báo lỗi xe");
            await T(0, "EB-1031");
            await C("Khóa không mở");
            await T(1, "Quét QR nhưng khóa xe không mở, đèn báo nhấp nháy đỏ.");
            await Shot("Form báo lỗi phương tiện",
                "Form báo lỗi: chọn đối tượng (phương tiện/điểm sạc), nhập mã xe EB-1031, chọn loại sự cố “Khóa không mở”, mô tả và (tùy chọn) đính kèm ảnh tối đa 5 MB.");
            await C("Demo: thử chọn ảnh 7,2 MB");
            await Dialog("Ngoại lệ 3a – Ảnh vượt giới hạn 5 MB",
                "Ảnh đính kèm 7,2 MB vượt giới hạn 5 MB nên không được tải lên; sinh viên được yêu cầu chọn ảnh nhỏ hơn.");
            await CloseDialog();
            await C("Gửi báo cáo");
            await Shot("Ngoại lệ 4a – Báo cáo được gộp vào sự cố đã có",
                "Xe EB-1031 đã có sự cố BC-1187 đang xử lý, nên báo cáo mới được liên kết vào sự cố hiện có thay vì tạo trùng; sinh viên nhận mã báo cáo để theo dõi.");

            // ===== UC-04 =====
            _kit.Section = "UC-04 Đặt chỗ đỗ xe cá nhân (Reserve parking)";
            await Tab(NavTab.Home);
            await C("Đặt chỗ đỗ xe");
            await Shot("Form đặt chỗ đỗ xe",
                "Chủ xe điện cá nhân chọn Hub (kèm số chỗ trống), xe cá nhân, ngày, giờ đến (hoặc bật “Đến ngay”) và thời lượng dự kiến.");
            toggles.ParkingHubFull = true;
            await C("Xác nhận đặt chỗ");
            await Shot("Ngoại lệ 4a – Hub đã hết chỗ",
                "Hub được chọn vừa hết chỗ: hệ thống gợi ý các Hub còn chỗ trong bán kính 1 km để sinh viên chọn lại hoặc đổi thời gian.");
            toggles.ParkingHubFull = false;
            await C("Đổi thời gian");
            await C("Xác nhận đặt chỗ");
            await Shot("Đặt chỗ đỗ xe thành công",
                "Đặt chỗ thành công: hiển thị Hub, số chỗ đỗ được cấp, khung giờ và mã QR để vào bãi.");
            await Tab(NavTab.Home);
            await C("Đặt chỗ đỗ xe");
            await C("Xác nhận đặt chỗ");
            await Dialog("Ngoại lệ 4b – Vượt giới hạn đặt chỗ đỗ",
                "Mỗi tài khoản chỉ có 1 đặt chỗ đỗ xe đang hoạt động, nên yêu cầu thứ hai bị từ chối.");
            await CloseDialog();

            // ===== UC-09 =====
            _kit.Section = "UC-09 Lên lịch sạc xe cá nhân (Schedule charging)";
            await Tab(NavTab.Home);
            await C("Lên lịch sạc");
            await Shot("Form lên lịch sạc",
                "Chọn Hub, xe cá nhân, khung giờ bắt đầu (hoặc “Sạc sớm nhất có thể”), thời lượng dự kiến và mức pin mục tiêu.");
            await C("Thư viện Trung tâm");
            await C("150 phút");
            await C("Xác nhận lịch sạc");
            await Dialog("Ngoại lệ 4b – Thời lượng vượt giới hạn của Hub",
                "Hub Thư viện Trung tâm chỉ cho sạc tối đa 120 phút, nên lịch 150 phút bị từ chối.");
            await CloseDialog();
            await C("Ga Metro Suối Tiên");
            await C("90 phút");
            toggles.NoChargingPointInWindow = true;
            await C("Xác nhận lịch sạc");
            await Shot("Ngoại lệ 4a – Không còn cổng sạc trong khung giờ",
                "Khung giờ đã chọn không còn cổng sạc: hệ thống gợi ý khung giờ hoặc Hub khác còn trống.");
            toggles.NoChargingPointInWindow = false;
            await C("Chọn lại");
            await C("Xác nhận lịch sạc");
            await Shot("Lên lịch sạc thành công",
                "Lịch sạc được tạo: Hub, cổng sạc được cấp, khung giờ và chi phí dự kiến. Hệ thống sẽ nhắc trước 30 phút.");

            // ===== UC-05 =====
            _kit.Section = "UC-05 Hủy / thay đổi đặt chỗ (Cancel or change booking)";
            await Tab(NavTab.Booking);
            await Shot("Đặt chỗ của tôi",
                "Tab “Đặt chỗ” liệt kê các đặt chỗ đang hoạt động (chỗ đỗ, lịch sạc, yêu cầu sạc) với nút Đổi giờ / Hủy tương ứng.");
            await C("Đổi giờ");
            await C("Ngày mai");
            await C("09:00");
            await Shot("Đổi giờ đặt chỗ",
                "Màn hình đổi giờ: chọn ngày, giờ bắt đầu mới và thời lượng; hệ thống kiểm tra còn chỗ/cổng trước khi cập nhật.");
            await C("Kiểm tra và cập nhật");
            await Dialog("Đổi giờ thành công",
                "Khung giờ mới còn trống nên đặt chỗ được cập nhật và thông báo xác nhận được gửi cho sinh viên.");
            await CloseDialog();
            await Tab(NavTab.Booking);
            await C("Hủy");
            await Shot("Xác nhận hủy đặt chỗ",
                "Trước khi hủy, hệ thống hiển thị thông tin đặt chỗ và phí hủy (nếu hủy muộn trong vòng 5 phút trước giờ bắt đầu).");
            if (!_kit.Click(_phone, "Xác nhận hủy")) _kit.Click(_phone, "Hủy và trả phí");
            await ShotKit.Settle();
            await Dialog("Hủy đặt chỗ thành công",
                "Đặt chỗ được hủy, chỗ/cổng được giải phóng và sinh viên nhận thông báo xác nhận.");
            await CloseDialog();
            await Tab(NavTab.Booking);
            await C("Đã kết thúc");
            await Shot("Đặt chỗ đã kết thúc",
                "Tab “Đã kết thúc” lưu lại các đặt chỗ đã hoàn tất, đã hủy hoặc hết hạn.");

            // ===== UC-11 =====
            _kit.Section = "UC-11 Thông báo và lịch sử (Notifications & history)";
            await Tab(NavTab.Home);
            await C("Hoạt động", prefix: true);
            await Shot("Thông báo",
                "Mục Hoạt động – Thông báo: các thông báo mới nhất ở trên cùng (đặt xe, cổng sạc đã sẵn sàng, thanh toán, báo lỗi…), được đánh dấu đã đọc khi mở.");
            await C("Lịch sử");
            await Shot("Lịch sử 3 tháng",
                "Tab Lịch sử: các chuyến đi và đặt chỗ trong 3 tháng gần nhất kèm thời gian, Hub, thời lượng, trạng thái và phí.");
            if (!_kit.Click(_phone, "Chuyến đi ", prefix: true)) _kit.ClickAfter(_phone, "Chuyến đi và đặt chỗ trong");
            await ShotKit.Settle();
            await Shot("Chi tiết hoạt động",
                "Bấm một chuyến đi trong lịch sử để xem chi tiết: thời gian, Hub nhận/trả, quãng đường và hóa đơn thanh toán.");

            // ===== UC-03 & UC-06: các ngoại lệ còn lại =====
            _kit.Section = "UC-03 Đặt trước phương tiện (Reserve vehicle)";
            await Tab(NavTab.Home);
            await C("Hub Ga Metro Suối Tiên");
            toggles.AnotherStudentReservesFirst = true;
            await C("EB-1040");
            await C("Đặt trước");
            // Xung đột có thể được phát hiện ngay khi bấm "Đặt trước" (hộp thoại hiện luôn) — chỉ bấm "Xác nhận đặt" nếu màn hình xác nhận còn hiện.
            if (Visible("Xác nhận đặt")) await C("Xác nhận đặt");
            await Dialog("Ngoại lệ 2a – Xe vừa được sinh viên khác đặt",
                "Hai sinh viên cùng đặt một xe: người xác nhận sau nhận thông báo xe đã được đặt và quay về danh sách để chọn xe khác.");
            toggles.AnotherStudentReservesFirst = false;
            await C("Về danh sách xe", last: true);
            await C("EM-2072");
            await C("Đặt trước");
            await C("Xác nhận đặt");

            _kit.Section = "UC-06 Nhận xe (Pick up vehicle)";
            toggles.UnlockTimeout = true;
            await C("Quét QR để nhận xe");
            await C("Giả lập quét", prefix: true);
            await Shot("Ngoại lệ 4a – Xe không phản hồi lệnh mở khóa",
                "Giả lập xe EM-2072 không phản hồi lệnh mở khóa: hệ thống tiếp tục chờ đến hết 30 giây.");
            await C("Bỏ qua thời gian chờ (demo)");
            await Task.Delay(1500);
            await ShotKit.Settle();
            await Shot("Không mở khóa được – tạo sự cố",
                "Hết 30 giây không có xác nhận: lệnh mở khóa bị hủy, xe vẫn được giữ cho sinh viên, hệ thống tự tạo sự cố gửi người điều hành và hiển thị thông tin liên hệ hỗ trợ.");
            toggles.UnlockTimeout = false;

            _kit.Section = "UC-03 Đặt trước phương tiện (Reserve vehicle)";
            await C("Về đặt chỗ");
            DemoApp.Clock.FastForward(16);
            DemoApp.Tick();
            await ShotKit.Settle();
            await Dialog("Luồng 5a – Hết 15 phút giữ xe, đặt chỗ tự hủy",
                "Quá 15 phút mà sinh viên chưa nhận xe: đặt chỗ tự động bị hủy, xe trở về trạng thái sẵn sàng và sinh viên được gợi ý tìm xe khác.");
            await CloseDialog();

            // ===== Tài khoản =====
            _kit.Section = "Màn hình khác";
            await Tab(NavTab.Account);
            await Shot("Tài khoản",
                "Màn hình Tài khoản của sinh viên (vai trò Sinh viên và Chủ xe điện cá nhân) với tùy chọn Chế độ tối.");

            // ===== Tùy chỉnh chế độ giao diện =====
            _kit.Section = "Tùy chỉnh chế độ giao diện";
            Theme.IsDark = true;
            Shell.Refresh();
            await ShotKit.Settle();
            await Shot("Chế độ tối – Tài khoản",
                "Bật “Chế độ tối” trong Tài khoản: toàn bộ ứng dụng chuyển sang bảng màu tối ngay lập tức.");
            await Tab(NavTab.Home);
            await Shot("Chế độ tối – Trang chủ",
                "Trang chủ ở chế độ tối: thẻ tìm điểm đến, bản đồ mini và danh sách Hub dùng bảng màu tối.");
            Theme.IsDark = false;
            Shell.Refresh();
        }
    }
}
