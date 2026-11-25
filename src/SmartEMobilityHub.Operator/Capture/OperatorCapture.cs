using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartEMobilityHub.Operator.ViewModels;

namespace SmartEMobilityHub.Capture
{
    /// <summary>
    /// Kịch bản chụp màn hình Trung tâm điều hành (Operator Dashboard).
    /// Chạy: dotnet run --project src\SmartEMobilityHub.Operator -- --capture demo_screens\operator
    /// </summary>
    internal static class OperatorCapture
    {
        private const double DesignWidth = 1520;
        private const double DesignHeight = 900;

        public static async Task RunAsync(Window window, string dir)
        {
            var kit = new ShotKit(dir);
            // Cố định kích thước thiết kế (không phụ thuộc độ phân giải màn hình) rồi thu nhỏ hiển thị bằng Viewbox.
            var content = (FrameworkElement)window.Content;
            window.Content = null;
            content.Width = DesignWidth;
            content.Height = DesignHeight;
            window.Content = new Viewbox { Stretch = Stretch.Uniform, Child = content };
            await ShotKit.Settle(800);

            MainViewModel Vm() => (MainViewModel)window.DataContext;

            string[] headers =
            {
                "Tình trạng vận hành", "Kỹ thuật viên", "Hàng chờ sạc HUB-08", "Cảnh báo gần đây", "Kịch bản demo",
                "Giám sát mạng lưới Hub", "Điều phối phương tiện", "Nhật ký hệ thống"
            };

            async Task Shot(string title, string caption)
            {
                Vm().Refresh();
                await ShotKit.Settle(400);
                kit.Save(content, title, caption, 1.5);
                // Kèm ảnh toàn bộ cột bên phải (cột cuộn) + vị trí các tiêu đề mục để ghép ảnh báo cáo.
                try
                {
                    var col = ShotKit.Flatten(content).OfType<ScrollViewer>()
                        .Where(sv => sv.TemplatedParent == null && sv.Content is FrameworkElement)
                        .OrderByDescending(sv => sv.ExtentHeight)
                        .Select(sv => (FrameworkElement)sv.Content)
                        .FirstOrDefault();
                    if (col == null) return;
                    var name = $"{kit.Count:00}_col";
                    ShotKit.SavePng(col, System.IO.Path.Combine(kit.Dir, name + ".png"), 1.5);
                    var marks = new Dictionary<string, double>();
                    foreach (var tb in ShotKit.Flatten(col).OfType<TextBlock>())
                    {
                        if (tb.Text != null && headers.Contains(tb.Text) && !marks.ContainsKey(tb.Text))
                            marks[tb.Text] = tb.TranslatePoint(new Point(0, 0), col).Y;
                    }
                    marks["__height"] = col.ActualHeight;
                    System.IO.File.WriteAllText(System.IO.Path.Combine(kit.Dir, name + ".json"), System.Text.Json.JsonSerializer.Serialize(marks));
                }
                catch (Exception ex)
                {
                    kit.Log("Lỗi chụp cột: " + ex.Message);
                }
            }

            try
            {
                kit.Section = "Trung tâm điều hành – Giám sát & cảnh báo (UC-SYS-01, UC-SYS-02)";
                Vm().ScanNow();
                await Shot("Tổng quan Trung tâm điều hành",
                    "Màn hình người điều hành sau một chu kỳ quét: danh sách sự cố bên trái, tình trạng vận hành (KPI, Hub quá tải, cảnh báo, kỹ thuật viên, hàng chờ sạc) ở giữa và giám sát mạng lưới Hub.");

                kit.Section = "UC-OP-03 Quản lý sự cố & bảo trì";
                Vm().SelectedIncident = Vm().Incidents.FirstOrDefault();
                await Shot("Chọn một sự cố",
                    "Người điều hành chọn một sự cố: xem đối tượng, Hub, người báo, mô tả, phiếu bảo trì và nhật ký xử lý; các nút Khóa thiết bị, Tạo phiếu bảo trì, Gán KTV gần nhất, Hoàn tất, Đóng sự cố, Tạm dừng Hub.");
                kit.LogResult("Khoá thiết bị", Vm().LockSelected());
                kit.LogResult("Tạo phiếu bảo trì", Vm().CreateTaskSelected());
                kit.LogResult("Gán KTV gần nhất", Vm().AssignSelected());
                await Shot("Khóa thiết bị, tạo phiếu bảo trì và gán kỹ thuật viên",
                    "Sau khi khóa thiết bị, tạo phiếu bảo trì và gán kỹ thuật viên gần nhất đang sẵn sàng, sự cố chuyển sang “Đang sửa”; mọi thao tác được ghi vào nhật ký xử lý.");
                kit.LogResult("Hoàn tất xử lý", Vm().ResolveSelected());
                kit.LogResult("Đóng sự cố", Vm().CloseSelected());
                await Shot("Hoàn tất và đóng sự cố",
                    "Kỹ thuật viên sửa xong: người điều hành bấm Hoàn tất xử lý rồi Đóng sự cố; thiết bị được mở lại và số liệu KPI cập nhật.");

                kit.Section = "Trung tâm điều hành – Giám sát & cảnh báo (UC-SYS-01, UC-SYS-02)";
                Vm().BreakPort();
                await Shot("Cảm biến báo cổng sạc CS-08-08 lỗi",
                    "Giả lập cổng sạc CS-08-08 báo lỗi: chu kỳ cập nhật trạng thái phát hiện lỗi, tạo cảnh báo và sự cố mới cho người điều hành.");
                Vm().DisconnectHub();
                Vm().MakeStale();
                await Shot("Hub mất kết nối và dữ liệu cũ",
                    "Giả lập HUB-03 mất kết nối và dữ liệu HUB-08 quá 10 phút chưa cập nhật: Hub được đánh dấu OFFLINE / dữ liệu cũ và hiện cảnh báo.");
                Vm().BreakChannel();
                await Shot("Mạng gửi cảnh báo bị lỗi – cảnh báo vào hàng đợi",
                    "Khi kênh gửi cảnh báo bị lỗi, cảnh báo quá tải HUB-08 không mất mà được đưa vào hàng đợi gửi lại.");
                Vm().RestoreChannel();
                await Shot("Khôi phục mạng và gửi lại cảnh báo",
                    "Mạng được khôi phục: sau 30 giây hệ thống tự gửi lại các cảnh báo đang chờ.");

                kit.Section = "UC-SYS-03 Lập lịch sạc thông minh";
                Vm().AddChargingRequests();
                await Shot("Hàng chờ sạc HUB-08",
                    "Thêm 3 yêu cầu sạc tại HUB-08: bộ lập lịch sạc thông minh xếp thứ tự ưu tiên theo mức pin và thời điểm cần xe, rồi gán cổng sạc trống.");

                kit.Section = "UC-OP-04 Điều phối phương tiện";
                var suggestError = Vm().SuggestAndCreateDispatch();
                if (suggestError != null)
                {
                    // Không có gợi ý tự động: tạo lệnh thủ công từ Hub đang quá tải (HUB-08) sang Hub còn trống.
                    kit.Log("Gợi ý tự động: " + suggestError + " → tạo lệnh thủ công.");
                    string? err = null;
                    foreach (var (src, dst) in new[] { ("HUB-08", "HUB-03"), ("HUB-08", "HUB-07"), ("HUB-12", "HUB-03"), ("HUB-09", "HUB-05"), ("HUB-01", "HUB-02") })
                    {
                        Vm().SourceHubId = src;
                        Vm().TargetHubId = dst;
                        foreach (var n in new[] { "3", "2", "1" })
                        {
                            Vm().DispatchVehicleCountText = n;
                            err = Vm().CreateDispatchOrder();
                            if (err == null) break;
                        }
                        if (err == null) break;
                    }
                    kit.LogResult("Tạo lệnh thủ công", err);
                }
                Vm().SelectedDispatchOrder = Vm().DispatchOrders.FirstOrDefault();
                await Shot("Gợi ý và tạo lệnh điều phối",
                    "Người điều hành chọn Hub nguồn (đang quá tải), Hub đích còn trống, số xe và nhân sự rồi tạo lệnh điều phối; lệnh ở trạng thái chờ xuất phát.");
                kit.LogResult("Gửi lệnh", Vm().ExecuteSelectedDispatch());
                await Shot("Gửi lệnh điều phối",
                    "Người điều hành gửi lệnh: các xe được chuyển sang trạng thái “Đang điều chuyển”, lệnh chuyển sang “Đang thực hiện”.");
                kit.LogResult("Hoàn tất lệnh", Vm().CompleteSelectedDispatch());
                await Shot("Hoàn tất điều phối",
                    "Nhân sự báo đã đưa xe tới Hub đích: lệnh hoàn tất và số xe của hai Hub được cập nhật trên màn hình giám sát.");

                // Ảnh toàn bộ nội dung từng cột cuộn (để thấy phần bị che khi cuộn).
                kit.Section = "Trung tâm điều hành – toàn bộ nội dung";
                Vm().Refresh();
                await ShotKit.Settle(400);
                var scrolls = ShotKit.Flatten(content).OfType<ScrollViewer>()
                    .Where(s => s.IsVisible && s.Content is FrameworkElement && s.ExtentHeight > s.ViewportHeight + 1)
                    .ToList();
                var i = 0;
                foreach (var sv in scrolls)
                {
                    if (sv.TemplatedParent != null) continue; // bỏ ScrollViewer nằm trong ListBox/TextBox
                    i++;
                    kit.Save((FrameworkElement)sv.Content, $"Toàn bộ nội dung cột cuộn {i}",
                        "Ảnh ghép toàn bộ nội dung của cột (bao gồm phần phải cuộn mới thấy).", 1.5);
                }
            }
            catch (Exception ex)
            {
                kit.Log("Dừng kịch bản do lỗi: " + ex);
            }
            kit.Finish();
            Application.Current.Shutdown();
        }
    }
}
