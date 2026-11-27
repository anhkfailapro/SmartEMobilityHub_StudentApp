using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartEMobilityHub.Operator;
using SmartEMobilityHub.Operator.Themes;
using SmartEMobilityHub.Operator.ViewModels;

namespace SmartEMobilityHub.Capture
{
    /// <summary>
    /// Kịch bản chụp màn hình Trung tâm điều hành (Operator Dashboard) theo từng trang của thanh bên.
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

            async Task Shot(int page, string title, string caption)
            {
                Vm().Refresh();
                Vm().PageIndex = page;
                await ShotKit.Settle(450);
                kit.Save(content, title, caption, 1.5);
            }

            try
            {
                // ===== Tổng quan =====
                kit.Section = "Trung tâm điều hành – Tổng quan mạng lưới (UC-SYS-02, UC-SYS-03)";
                Vm().ScanNow();
                await Shot(MainViewModel.PageOverview, "Tổng quan mạng lưới Mobility Hub",
                    "Sau một chu kỳ quét: KPI toàn mạng (Hub hoạt động, xe sẵn sàng, cổng sạc trống, Hub quá tải, sự cố, cảnh báo), thẻ từng Hub với mức sử dụng chỗ đỗ và cổng sạc, danh sách Hub có nguy cơ quá tải và cảnh báo gần đây.");

                // ===== Giám sát xe & cổng sạc =====
                kit.Section = "Trung tâm điều hành – Giám sát phương tiện & cổng sạc";
                await Shot(MainViewModel.PageAssets, "Giám sát phương tiện và cổng sạc",
                    "Bảng toàn bộ xe (mức pin, trạng thái, Hub/vị trí, thời điểm cập nhật cảm biến; dữ liệu quá 10 phút tô cam) và lưới cổng sạc theo Hub (trống, đang sạc, lỗi, offline, công suất đang dùng / giới hạn).");
                Vm().OpenHubAssets("HUB-08");
                await ShotKit.Settle(300);
                kit.Save(content, "Lọc theo Hub HUB-08",
                    "Bấm thẻ Hub ở trang Tổng quan (hoặc chọn Hub ở bộ lọc) để chỉ xem xe và cổng sạc của Hub đó.", 1.5);

                // ===== UC-OP-03 =====
                kit.Section = "UC-OP-03 Quản lý sự cố & bảo trì";
                Vm().Incidents.SelectedIncident = Vm().Incidents.Incidents.FirstOrDefault();
                await Shot(MainViewModel.PageIncidents, "Chọn một sự cố",
                    "Người điều hành chọn một sự cố: xem đối tượng, Hub, người báo, mô tả, phiếu bảo trì và nhật ký xử lý; các nút Khóa thiết bị, Tạo phiếu bảo trì, Gán KTV gần nhất, Hoàn tất, Đóng sự cố, Tạm dừng Hub.");
                kit.LogResult("Khoá thiết bị", Vm().Incidents.LockSelected());
                kit.LogResult("Tạo phiếu bảo trì", Vm().Incidents.CreateTaskSelected());
                kit.LogResult("Gán KTV gần nhất", Vm().Incidents.AssignSelected());
                await Shot(MainViewModel.PageIncidents, "Khóa thiết bị, tạo phiếu bảo trì và gán kỹ thuật viên",
                    "Sau khi khóa thiết bị, tạo phiếu bảo trì và gán kỹ thuật viên gần nhất đang sẵn sàng, sự cố chuyển sang “Đang sửa”; mọi thao tác được ghi vào nhật ký xử lý.");
                kit.LogResult("Hoàn tất xử lý", Vm().Incidents.ResolveSelected());
                kit.LogResult("Đóng sự cố", Vm().Incidents.CloseSelected());
                await Shot(MainViewModel.PageIncidents, "Hoàn tất và đóng sự cố",
                    "Kỹ thuật viên sửa xong: người điều hành bấm Hoàn tất xử lý rồi Đóng sự cố; thiết bị được mở lại và số liệu KPI cập nhật.");

                // ===== Sự kiện IoT =====
                kit.Section = "Trung tâm điều hành – Giám sát & cảnh báo (UC-SYS-01, UC-SYS-02)";
                Vm().Demo.BreakPort();
                Vm().OpenHubAssets("HUB-08");
                await Shot(MainViewModel.PageAssets, "Cảm biến báo cổng sạc CS-08-08 lỗi",
                    "Giả lập cổng sạc CS-08-08 báo lỗi: cổng chuyển sang trạng thái Lỗi trong lưới cổng sạc của HUB-08, đồng thời hệ thống tạo cảnh báo và sự cố mới cho người điều hành.");
                Vm().Demo.DisconnectHub();
                Vm().Demo.MakeStale();
                await Shot(MainViewModel.PageOverview, "Hub mất kết nối và dữ liệu cũ",
                    "Giả lập HUB-03 mất kết nối và dữ liệu HUB-08 quá 10 phút chưa cập nhật: thẻ Hub đổi sang “Mất kết nối”, cảnh báo dữ liệu cũ xuất hiện ở cột bên phải.");
                Vm().Demo.BreakChannel();
                await Shot(MainViewModel.PageDemo, "Mạng gửi cảnh báo bị lỗi – cảnh báo vào hàng đợi",
                    "Khi kênh gửi cảnh báo bị lỗi, cảnh báo quá tải HUB-08 không mất mà được đưa vào hàng đợi gửi lại (trạng thái “Chờ gửi lại”).");
                Vm().Demo.RestoreChannel();
                await Shot(MainViewModel.PageDemo, "Khôi phục mạng và gửi lại cảnh báo",
                    "Mạng được khôi phục: sau 30 giây hệ thống tự gửi lại các cảnh báo đang chờ; nhật ký hệ thống ghi lại từng bước.");

                // ===== Sạc thông minh =====
                kit.Section = "UC-SYS-01 / UC-OP-04 Lập lịch sạc thông minh";
                Vm().Charging.AddChargingRequests();
                await Shot(MainViewModel.PageCharging, "Hàng chờ sạc HUB-08",
                    "Thêm 3 yêu cầu sạc tại HUB-08: bộ lập lịch xếp thứ tự theo điểm ưu tiên (pin thấp, thời điểm cần xe, cao điểm), gán cổng trống trong giới hạn công suất; lưới cổng sạc của Hub hiện ngay bên dưới.");

                // ===== Điều phối =====
                kit.Section = "UC-OP-04 Điều phối phương tiện";
                var suggestError = Vm().Dispatch.SuggestAndCreateDispatch();
                if (suggestError != null)
                {
                    kit.Log("Gợi ý tự động: " + suggestError + " → tạo lệnh thủ công.");
                    string? err = null;
                    foreach (var (src, dst) in new[] { ("HUB-08", "HUB-03"), ("HUB-08", "HUB-07"), ("HUB-12", "HUB-03"), ("HUB-09", "HUB-05"), ("HUB-01", "HUB-02") })
                    {
                        Vm().Dispatch.SourceHubId = src;
                        Vm().Dispatch.TargetHubId = dst;
                        foreach (var n in new[] { "3", "2", "1" })
                        {
                            Vm().Dispatch.DispatchVehicleCountText = n;
                            err = Vm().Dispatch.CreateDispatchOrder();
                            if (err == null) break;
                        }
                        if (err == null) break;
                    }
                    kit.LogResult("Tạo lệnh thủ công", err);
                }
                Vm().Dispatch.SelectedDispatchOrder = Vm().Dispatch.DispatchOrders.FirstOrDefault();
                await Shot(MainViewModel.PageDispatch, "Gợi ý và tạo lệnh điều phối",
                    "Bên trái: mức cân bằng tải của từng Hub (dư xe / cân bằng / thiếu xe). Người điều hành bấm Gợi ý tự động (hoặc chọn Hub nguồn, Hub đích, số xe) để tạo lệnh; lệnh ở trạng thái Chờ xuất phát.");
                kit.LogResult("Xuất phát", Vm().Dispatch.ExecuteSelectedDispatch());
                await Shot(MainViewModel.PageDispatch, "Lệnh điều phối đang thực hiện",
                    "Bấm Xuất phát: các xe chuyển sang trạng thái “Đang điều chuyển”, lệnh chuyển sang “Đang điều phối”.");
                kit.LogResult("Hoàn tất lệnh", Vm().Dispatch.CompleteSelectedDispatch());
                await Shot(MainViewModel.PageDispatch, "Hoàn tất điều phối",
                    "Nhân sự báo đã đưa xe tới Hub đích: lệnh hoàn tất và số xe, mức sử dụng của hai Hub được cập nhật.");

                // ===== What-if =====
                kit.Section = "UC-OP-05 Mô phỏng & phân tích kịch bản (What-if)";
                await CaptureWhatIf(window, kit);

                // ===== Chế độ tối =====
                kit.Section = "Tùy chỉnh chế độ giao diện";
                OperatorTheme.SetDark(true);
                await Shot(MainViewModel.PageOverview, "Chế độ tối – Tổng quan",
                    "Nút “Chế độ tối” ở thanh tiêu đề đổi toàn bộ giao diện Trung tâm điều hành (kể cả cửa sổ đăng nhập và What-if) sang bảng màu tối ngay lập tức.");
                await Shot(MainViewModel.PageAssets, "Chế độ tối – Phương tiện & cổng sạc",
                    "Nhãn trạng thái và ô cổng sạc tự đổi sang bảng màu tối.");
                OperatorTheme.SetDark(false);
            }
            catch (Exception ex)
            {
                kit.Log("Dừng kịch bản do lỗi: " + ex);
            }
            kit.Finish();
            Application.Current.Shutdown();
        }

        private static async Task CaptureWhatIf(Window owner, ShotKit kit)
        {
            if (owner is not MainWindow main)
            {
                kit.Log("Không lấy được OperatorDemo để mở What-if.");
                return;
            }
            // What-if mở ngay trong cửa sổ chính (không còn cửa sổ riêng): chụp cả cửa sổ để thấy thanh bên.
            var page = main.ShowWhatIf();
            await ShotKit.Settle(700);
            var vm = page.ViewModel;
            var root = (FrameworkElement)main.Content;
            try
            {
                vm.SelectedPreset = vm.Presets.First();
                vm.RunSimulation();
                await ShotKit.Settle(500);
                kit.Save(root, "Kết quả mô phỏng: lượng sinh viên tăng đột biến tại ga Metro",
                    "Chọn kịch bản và Hub, đặt mức tăng nhu cầu, khung thời gian và bước thời gian rồi chạy: biểu đồ dự báo mức sử dụng chỗ đỗ / cổng sạc theo từng bước, tác động lên các Hub, cảnh báo quá tải và các đề xuất điều phối.", 1.2);
                var rec = vm.Recommendations.FirstOrDefault();
                if (rec != null)
                {
                    vm.ApplyRecommendation(rec.Id);
                    await ShotKit.Settle(500);
                    kit.Save(root, "Mô phỏng lại sau khi áp dụng đề xuất",
                        $"Bấm “Mô phỏng” cạnh đề xuất “{rec.Title}”: hệ thống tính lại trên bản sao trạng thái và so sánh trước/sau, dữ liệu vận hành thật không thay đổi.", 1.2);
                }
                var outage = vm.Presets.Skip(3).FirstOrDefault();
                if (outage != null)
                {
                    vm.SelectedPreset = outage;
                    vm.RunSimulation();
                    await ShotKit.Settle(500);
                    kit.Save(root, $"Kịch bản: {outage.Name}",
                        "Một kịch bản khác (một phần cổng sạc hỏng): hệ thống đánh giá ảnh hưởng tới năng lực sạc và đề xuất ưu tiên xe pin thấp / chuyển người dùng sang Hub thay thế.", 1.2);
                }
            }
            catch (Exception ex)
            {
                kit.Log("Lỗi chụp What-if: " + ex.Message);
            }
            finally
            {
                main.HideWhatIf();
            }
        }
    }
}
