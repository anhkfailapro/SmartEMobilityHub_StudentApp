using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;

namespace SmartEMobilityHub.Screens
{
    public static class PickUpFlow
    {
        public static void Verify(DemoApp app, string vehicleCode, string? pickUpCode)
        {
            var shell = app.Shell;
            var result = app.PickUp.VerifyPickUpCode(vehicleCode, pickUpCode);
            if (result.Ok)
            {
                shell.Navigate(new UnlockingScreen(app, result.Value!));
                return;
            }
            switch (result.Error)
            {
                case "Wrong":
                    shell.ShowDialog(Tone.Warning, "Sai xe", result.Message, new DialogAction("Thử lại", ButtonVariant.Primary));
                    break;
                case "NoAttempts":
                    shell.ShowDialog(Tone.Danger, "Hết lượt thử", result.Message, new DialogAction("Đóng", ButtonVariant.Outline));
                    break;
                case "Expired":
                    shell.ShowDialog(Tone.Danger, "Đặt chỗ đã được giải phóng", result.Message + " Bạn có muốn tìm xe khác?",
                        new DialogAction("Tìm xe khác", ButtonVariant.Primary, () => app.GoTab(NavTab.Home)),
                        new DialogAction("Đóng", ButtonVariant.Outline, () => app.GoTab(NavTab.Home)));
                    break;
                case "NotAtHub":
                    shell.ShowDialog(Tone.Warning, "Bạn chưa ở Hub", result.Message);
                    break;
                default:
                    shell.ShowDialog(Tone.Info, "Không thể nhận xe", result.Message,
                        new DialogAction("Về trang chủ", ButtonVariant.Primary, () => app.GoTab(NavTab.Home)));
                    break;
            }
        }
    }
}
