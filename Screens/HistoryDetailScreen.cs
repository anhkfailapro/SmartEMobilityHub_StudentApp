using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class HistoryDetailScreen : Screen
    {
        private readonly HistoryEntry _entry;

        public HistoryDetailScreen(HistoryEntry entry)
        {
            _entry = entry;
        }

        public override string? Title => "Chi tiết hoạt động";

        public override UIElement BuildContent()
        {
            var root = Stack(16, Card(
                Text(_entry.Title, TextKind.Subtitle),
                Row("Thời gian", $"{_entry.Date:dd/MM/yyyy HH:mm}"),
                Row("Hub", _entry.HubName),
                Row("Thời lượng", _entry.Duration),
                Row("Trạng thái", _entry.Status),
                Row("Phí", _entry.Fee, Theme.Primary)));
            if (_entry.Trip?.Invoice != null)
                Add(root, InvoiceView.Build(_entry.Trip), 16);
            else
                Add(root, Text("Hoạt động này không có hóa đơn thanh toán.", TextKind.Body, Theme.Text2), 16);
            return root;
        }
    }
}
