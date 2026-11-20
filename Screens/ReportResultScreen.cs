using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ReportResultScreen : Screen
    {
        private readonly Incident _incident;
        private readonly bool _linked;
        private readonly Trip? _returnedTrip;

        public ReportResultScreen(Incident incident, bool linked, Trip? returnedTrip)
        {
            _incident = incident;
            _linked = linked;
            _returnedTrip = returnedTrip;
        }

        public override string? Title => "Báo lỗi";
        public override bool ShowBack => false;

        public override UIElement BuildContent()
        {
            var tile = IconTile(Icons.Check, Tone.Success, 72);
            tile.HorizontalAlignment = HorizontalAlignment.Center;
            var root = Stack(16, tile,
                Centered(Text(_linked ? "Báo cáo đã được gộp" : "Đã gửi báo cáo", TextKind.Title)),
                Card(
                    Row("Mã báo cáo", _incident.Code, Theme.Primary),
                    Row("Đối tượng", _incident.ObjectCode),
                    Row("Loại sự cố", _incident.ProblemType),
                    Row("Trạng thái", "Đang kiểm tra", Theme.Danger),
                    Row("Số báo cáo cho sự cố này", _incident.ReportCount.ToString())));
            Add(root, _linked
                ? Banner(Tone.Info, $"{_incident.ObjectCode} đã có sự cố đang xử lý ({_incident.Code}). Báo cáo của bạn đã được gộp vào sự cố này, không tạo sự cố mới.")
                : Banner(Tone.Info, "Đối tượng đã chuyển sang “Đang kiểm tra” và bị ẩn khỏi danh sách khả dụng. Người điều hành đã nhận sự cố."), 16);
            if (_returnedTrip?.Invoice != null)
            {
                var inv = _returnedTrip.Invoice;
                Add(root, Card(
                    Text("Chuyến đi đã kết thúc trước khi báo lỗi", TextKind.Subtitle),
                    Row("Hóa đơn", $"#{inv.Number}"),
                    Row("Tổng cộng", DemoConfig.Money(inv.Total), Theme.Primary)), 16);
            }
            return root;
        }

        public override UIElement? BuildActions() => Button("Về trang chủ", ButtonVariant.Primary, ScreenHelpers.GoHome);
    }
}
