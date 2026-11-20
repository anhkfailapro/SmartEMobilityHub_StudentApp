using System.Windows;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ManualCodeScreen : Screen
    {
        private string _vehicleCode = "";
        private string _pickUpCode = "";

        public override string? Title => "Nhập mã thủ công";

        public override UIElement BuildContent()
        {
            var r = DemoApp.Reservations.ActiveVehicleReservation;
            var root = Stack(16,
                Banner(Tone.Info, "Dùng khi mã QR trên xe bị hỏng. Nhập mã xe (dán trên thân xe) và mã nhận xe của bạn."),
                Labeled("Mã xe", InputBox("VD: EB-1024", _vehicleCode, v => _vehicleCode = v)),
                Labeled("Mã nhận xe", InputBox("VD: A7K-29", _pickUpCode, v => _pickUpCode = v)));
            if (r != null)
                Add(root, Text($"Gợi ý demo: mã xe {r.VehicleCode}, mã nhận xe {r.PickUpCode}.", TextKind.Caption, Theme.Text2), 16);
            return root;
        }

        public override UIElement? BuildActions() => Button("Xác nhận", ButtonVariant.Primary, () =>
        {
            if (string.IsNullOrWhiteSpace(_vehicleCode) || string.IsNullOrWhiteSpace(_pickUpCode))
            {
                Shell.ShowDialog(Tone.Info, "Thiếu thông tin", "Vui lòng nhập cả mã xe và mã nhận xe.");
                return;
            }
            PickUpFlow.Verify(_vehicleCode, _pickUpCode);
        });
    }
}
