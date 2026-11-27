using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ReportProblemScreen : Screen
    {
        private static readonly string[] VehicleProblems = { "Phanh", "Pin / Sạc", "Lốp xe", "Khóa không mở", "Đèn", "Khác" };
        private static readonly string[] PointProblems = { "Không sạc được", "Đầu cắm hỏng", "Màn hình lỗi", "Khác" };
        private ReportObjectKind _kind;
        private string _code;
        private string _hubId = CampusMap.DefaultHubId;
        private int _point = 1;
        private string _problem = "Pin / Sạc";
        private string _description = "";
        private string? _photoName;
        private double? _photoMb;

        public ReportProblemScreen(DemoApp app, ReportObjectKind kind = ReportObjectKind.Vehicle, string? code = null) : base(app)
        {
            _kind = kind;
            _code = code ?? "";
        }

        public override string? Title => "Báo lỗi";

        public override UIElement BuildContent()
        {
            var store = App.Store;
            var root = Stack(16, Labeled("Đối tượng", Segmented(new[] { "Phương tiện", "Điểm sạc" }, _kind == ReportObjectKind.Vehicle ? 0 : 1, i =>
            {
                _kind = i == 0 ? ReportObjectKind.Vehicle : ReportObjectKind.ChargingPoint;
                _problem = i == 0 ? VehicleProblems[1] : PointProblems[0];
                Shell.Refresh();
            })));

            if (_kind == ReportObjectKind.Vehicle)
            {
                Add(root, Labeled("Mã phương tiện", InputBox("VD: EB-1024", _code, v => _code = v)), 16);
            }
            else
            {
                var hub = store.GetHub(_hubId);
                Add(root, Labeled("Hub", ChipGroup(store.Hubs, hub, h => h.ShortName, h =>
                {
                    _hubId = h.Id;
                    _point = 1;
                    Shell.Refresh();
                })), 16);
                Add(root, Labeled("Cổng sạc", ChipGroup(hub.Points.Select(p => p.Number), _point, n => $"Cổng #{n}", n =>
                {
                    _point = n;
                    Shell.Refresh();
                })), 16);
            }

            Add(root, Labeled("Loại sự cố", ChipGroup(_kind == ReportObjectKind.Vehicle ? VehicleProblems : PointProblems, _problem, p => p, p =>
            {
                _problem = p;
                Shell.Refresh();
            })), 16);
            Add(root, Labeled("Mô tả", InputBox("Mô tả ngắn sự cố bạn gặp…", _description, v => _description = v, true)), 16);

            var photoText = _photoName == null ? "Thêm ảnh (tùy chọn, tối đa 5 MB)" : $"{_photoName} · {_photoMb:0.0} MB (nhấn để đổi)";
            var photo = Panel(HStack(8, Icon(Icons.Camera, 18, Theme.Text2), Text(photoText, TextKind.Subtitle, Theme.Text2)));
            photo.BorderBrush = Theme.BorderStrong;
            Add(root, OnClick(photo, PickPhoto), 16);
            var demoPhoto = Text("Demo: thử chọn ảnh 7,2 MB", TextKind.Caption, Theme.Primary);
            Add(root, OnClick(demoPhoto, () => RejectPhoto(7.2)), 8);

            Add(root, Banner(Tone.Info, _kind == ReportObjectKind.Vehicle
                ? "Phương tiện sẽ chuyển sang trạng thái “Đang kiểm tra” và được gửi tới người điều hành."
                : "Điểm sạc sẽ chuyển sang trạng thái “Ngưng” và được gửi tới người điều hành."), 16);
            return root;
        }

        public override UIElement? BuildActions() => Button("Gửi báo cáo", ButtonVariant.Danger, Submit);

        private void PickPhoto()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tất cả tệp|*.*", Title = "Chọn ảnh sự cố" };
            if (dialog.ShowDialog() != true) return;
            var mb = new System.IO.FileInfo(dialog.FileName).Length / 1024.0 / 1024.0;
            if (mb > DemoConfig.MaxPhotoMb)
            {
                RejectPhoto(mb);
                return;
            }
            _photoName = System.IO.Path.GetFileName(dialog.FileName);
            _photoMb = mb;
            Shell.Refresh();
        }

        private void RejectPhoto(double mb)
        {
            Shell.ShowDialog(Tone.Danger, "Ảnh quá lớn", $"Ảnh {mb:0.0} MB vượt giới hạn {DemoConfig.MaxPhotoMb:0} MB nên không được tải lên. Vui lòng chọn ảnh nhỏ hơn.");
        }

        private void Submit()
        {
            if (_kind == ReportObjectKind.Vehicle)
            {
                if (string.IsNullOrWhiteSpace(_code))
                {
                    Shell.ShowDialog(Tone.Info, "Thiếu mã phương tiện", "Vui lòng nhập mã phương tiện cần báo lỗi.");
                    return;
                }
                if (App.Store.GetVehicle(_code) == null)
                {
                    Shell.ShowDialog(Tone.Warning, "Không tìm thấy phương tiện", $"Không có phương tiện mã {_code.Trim()}.");
                    return;
                }
                var trip = App.Billing.ActiveTrip;
                if (trip != null && string.Equals(trip.VehicleCode, _code.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    Shell.ShowDialog(Tone.Warning, "Chuyến đi đang diễn ra",
                        $"Bạn đang dùng {trip.VehicleCode}. Hệ thống sẽ kết thúc chuyến (trả xe) trước, sau đó gửi báo cáo.",
                        new DialogAction("Kết thúc chuyến và báo lỗi", ButtonVariant.Danger, () => Shell.Navigate(new ReturnVehicleScreen(App, t => SendReport(t)))),
                        new DialogAction("Hủy", ButtonVariant.Outline));
                    return;
                }
            }
            SendReport(null);
        }

        private void SendReport(Trip? returnedTrip)
        {
            var hub = App.Store.GetHub(_hubId);
            var code = _kind == ReportObjectKind.Vehicle ? _code.Trim() : $"{hub.ShortName} · Cổng #{_point}";
            var result = App.Incidents.ReceiveStudentReport(_kind, code, _kind == ReportObjectKind.ChargingPoint ? _hubId : null,
                _kind == ReportObjectKind.ChargingPoint ? _point : null, _problem, _description, _photoMb);
            if (!result.Ok)
            {
                Shell.ShowDialog(Tone.Danger, "Không thể gửi báo cáo", result.Message);
                return;
            }
            Shell.ResetTo(new ReportResultScreen(App, result.Value!, result.Message == "Linked", returnedTrip));
        }
    }
}
