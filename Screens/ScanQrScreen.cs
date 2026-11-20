using System.Windows;
using System.Windows.Media;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ScanQrScreen : Screen
    {
        public override string? Title => "Quét mã QR";

        public override UIElement BuildContent()
        {
            var r = DemoApp.Reservations.ActiveVehicleReservation;
            var frame = new System.Windows.Shapes.Rectangle
            {
                Width = 200,
                Height = 200,
                Stroke = Brushes.White,
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 6, 3 },
                RadiusX = 16,
                RadiusY = 16
            };
            var camera = new System.Windows.Controls.Border
            {
                Height = 300,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Child = frame
            };
            return Stack(16,
                camera,
                Centered(Text(r == null ? "Không có đặt xe đang hoạt động." : $"Đưa mã QR trên xe vào khung để nhận xe {r.VehicleCode}.", TextKind.Body, Theme.Text2)),
                Banner(Tone.Info, "Demo: máy tính không có camera. Dùng nút bên dưới để giả lập quét đúng xe hoặc quét nhầm xe khác."));
        }

        public override UIElement? BuildActions()
        {
            var r = DemoApp.Reservations.ActiveVehicleReservation;
            var code = r?.VehicleCode ?? "EB-1024";
            var other = code == "EB-1040" ? "EB-1024" : "EB-1040";
            return Stack(12,
                Button($"Giả lập quét {code} (đúng xe)", ButtonVariant.Primary, () => PickUpFlow.Verify(code, null), r != null, Icons.Camera),
                Columns(12,
                    Button($"Quét nhầm {other}", ButtonVariant.Outline, () => PickUpFlow.Verify(other, null), r != null),
                    Button("Nhập mã thủ công", ButtonVariant.Secondary, () => Shell.Replace(new ManualCodeScreen()), r != null)));
        }
    }
}
