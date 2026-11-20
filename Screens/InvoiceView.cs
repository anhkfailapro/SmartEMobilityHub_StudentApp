using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public static class InvoiceView
    {
        public static UIElement Build(Trip t)
        {
            var store = DemoApp.Store;
            var inv = t.Invoice!;
            var rate = DemoConfig.RentalPricePerMinute;
            var duration = (t.EndTime ?? DemoApp.Clock.Now) - t.StartTime;
            var card = Card(
                Split(Text($"Hóa đơn #{inv.Number}", TextKind.Subtitle), Badge(inv.PaidByPlanOnly ? "Trừ gói tháng" : "Đã thanh toán", Tone.Success)),
                Row("Bắt đầu", $"{DemoConfig.Time(t.StartTime)} · {store.GetHub(t.StartHubId).ShortName}"),
                Row("Kết thúc", $"{(t.EndTime.HasValue ? DemoConfig.Time(t.EndTime.Value) : "-")} · {(t.EndHubId != null ? store.GetHub(t.EndHubId).ShortName : "-")}"),
                Row("Thời lượng", $"{(int)duration.TotalMinutes} phút {duration.Seconds} giây"),
                Row("Quãng đường", DemoConfig.Km(t.DistanceKm)),
                Divider(),
                Row($"Phí thuê ({inv.Minutes} phút × {DemoConfig.Money(rate)})", DemoConfig.Money(inv.RentalFee)));
            var stack = (System.Windows.Controls.StackPanel)card.Child!;
            if (inv.FreeExtraMinutes > 0)
                Add(stack, Row($"Miễn phí thêm ({inv.FreeExtraMinutes} phút)", "-" + DemoConfig.Money(inv.FreeExtraMinutes * rate), Theme.Success), 12);
            if (inv.PlanDeduction > 0)
                Add(stack, Row("Trừ phút miễn phí gói tháng", "-" + DemoConfig.Money(inv.PlanDeduction), Theme.Success), 12);
            Add(stack, Divider(), 12);
            Add(stack, Row("Tổng cộng", DemoConfig.Money(inv.Total), Theme.Primary), 12);
            Add(stack, HStack(8, Icon(Icons.Receipt, 14, Theme.Text2),
                Text(inv.PaidByPlanOnly ? "Trừ vào gói tháng, không cần thanh toán" : $"Thanh toán qua Payment Gateway · {inv.PaymentMethod}", TextKind.Caption, Theme.Text2)), 12);
            return card;
        }
    }
}
