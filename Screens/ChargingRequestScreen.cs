using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public class ChargingRequestScreen : Screen
    {
        private readonly string _vehicleCode;
        private readonly string _originalHubId;
        private string _hubId;
        private bool _showOthers;

        public ChargingRequestScreen(DemoApp app, string vehicleCode, string hubId) : base(app)
        {
            _vehicleCode = vehicleCode;
            _originalHubId = hubId;
            _hubId = hubId;
        }

        public override string? Title => "Yêu cầu sạc";

        private bool ApplyBusyToggle => _hubId == _originalHubId;

        public override UIElement BuildContent()
        {
            var v = App.Store.GetVehicle(_vehicleCode)!;
            var trip = App.Billing.ActiveTrip;
            var battery = trip != null && trip.VehicleCode == _vehicleCode ? App.Billing.CurrentBattery(trip) : v.Battery;
            var e = App.Charging.GetChargingEstimate(_vehicleCode, _hubId, ApplyBusyToggle);
            var hub = e.Hub;

            var root = Stack(16,
                Card(
                    Split(Text($"{v.Code} · {v.TypeLabel}", TextKind.Subtitle), Badge(battery < DemoConfig.NeedsChargeBelow ? "Pin yếu" : $"Pin {battery}%", battery < DemoConfig.NeedsChargeBelow ? Tone.Danger : Tone.Warning)),
                    HStack(10, Icon(Icons.Bolt, 26, Theme.Danger), Text($"{battery}%", TextKind.Big, null, false), Text($"~{(int)Math.Round(battery * (v.Type == VehicleType.EBike ? 0.46 : 0.78))} km còn lại", TextKind.Body, Theme.Text2, false)),
                    BatteryBar(battery)));

            var points = new System.Windows.Controls.Primitives.UniformGrid { Rows = 1, Columns = Math.Max(1, hub.Points.Count) };
            foreach (var p in hub.Points)
            {
                var (label, tone) = e.AllBusy && p.Status == PointStatus.Free
                    ? ("Bận", Tone.Charging)
                    : p.Status switch
                    {
                        PointStatus.Free => ("Trống", Tone.Success),
                        PointStatus.Charging => ("Đang sạc", Tone.Charging),
                        _ => ("Ngưng", Tone.Danger)
                    };
                var (bg, fg) = Theme.ToneBrushes(tone);
                points.Children.Add(new System.Windows.Controls.Border
                {
                    Background = bg,
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(2),
                    Padding = new Thickness(2, 8, 2, 8),
                    Child = Stack(4, Icon(Icons.Bolt, 14, fg), Centered(Text(label, TextKind.Caption, fg, false)))
                });
            }
            var priorityTone = e.Priority is PriorityLevel.Critical or PriorityLevel.High ? Theme.Danger : Theme.Text;
            Add(root, Card(
                Split(Text(hub.Name, TextKind.Subtitle), Text($"{e.FreePoints}/{hub.Points.Count} cổng trống", TextKind.Body, Theme.Text2, false)),
                points,
                Divider(),
                Row("Thời gian chờ ước tính", $"~{e.WaitMinutes} phút"),
                Row("Mức ưu tiên", $"{DemoConfig.PriorityLabel(e.Priority)} ({e.Score:0})", priorityTone),
                Row("Vị trí dự kiến trong hàng chờ", $"#{e.QueuePosition}")), 16);

            if (e.AllBusy)
                Add(root, Banner(Tone.Warning, $"Tất cả cổng sạc tại {hub.Name} đang bận hoặc ngưng hoạt động. Thời gian chờ ~{e.WaitMinutes} phút. Bạn có thể chọn Hub khác."), 16);

            var others = App.Charging.OtherHubs(_vehicleCode, _hubId);
            var best = others.FirstOrDefault();
            if (best != null)
            {
                var link = Panel(Split(Text($"Chọn Hub khác ({best.Hub.ShortName}: chờ ~{best.WaitMinutes} phút)", TextKind.Subtitle, Theme.Primary), Icon(_showOthers ? Icons.Close : Icons.ChevronRight, 14, Theme.Primary)));
                link.Padding = new Thickness(14);
                Add(root, OnClick(link, () =>
                {
                    _showOthers = !_showOthers;
                    Shell.Refresh();
                }), 16);
            }
            if (_showOthers)
            {
                foreach (var o in others)
                {
                    var id = o.Hub.Id;
                    Add(root, HubCard(o.Hub.Name, $"Chờ ~{o.WaitMinutes} phút · #{o.QueuePosition} · {o.FreePoints}/{o.Hub.Points.Count} cổng trống", DemoConfig.Km(o.Hub.DistanceKmTo(hub)), () =>
                    {
                        _hubId = id;
                        _showOthers = false;
                        Shell.Refresh();
                    }), 8);
                }
            }

            if (trip != null && trip.VehicleCode == _vehicleCode)
            {
                Add(root, Card(
                    Text("Hành động kết hợp", TextKind.Subtitle),
                    Text($"Kết thúc chuyến tại {hub.ShortName}, gửi yêu cầu sạc cho {_vehicleCode} và đặt một xe mới tại Hub này để đi tiếp.", TextKind.Body, Theme.Text2),
                    Button("Kết thúc chuyến, đặt xe mới và yêu cầu sạc", ButtonVariant.Charging, Combined)), 16);
            }
            return root;
        }

        public override UIElement? BuildActions() => Button("Xác nhận yêu cầu sạc", ButtonVariant.Primary, Submit);

        private OpResult<ChargingRequest> SubmitRequest()
        {
            var result = App.Charging.SubmitChargingRequest(_vehicleCode, _hubId, ApplyBusyToggle);
            if (result.Error == "Duplicate")
            {
                var existing = result.Value!;
                Shell.ShowDialog(Tone.Info, "Yêu cầu đã tồn tại", result.Message,
                    new DialogAction("Xem yêu cầu", ButtonVariant.Primary, () => Shell.Replace(new ChargingQueuedScreen(App, existing.Id))),
                    new DialogAction("Đóng", ButtonVariant.Outline));
            }
            else if (!result.Ok)
            {
                Shell.ShowDialog(Tone.Warning, "Không gửi được yêu cầu sạc", result.Message,
                    new DialogAction("Đóng", ButtonVariant.Outline));
            }
            return result;
        }

        private void Submit()
        {
            var result = SubmitRequest();
            if (result.Ok) Shell.Replace(new ChargingQueuedScreen(App, result.Value!.Id));
        }

        private void Combined()
        {
            var result = SubmitRequest();
            if (!result.Ok) return;
            var request = result.Value!;
            var hubId = _hubId;
            var hub = App.Store.GetHub(hubId);
            var note = request.State == ChargingRequestState.Assigned
                ? $"Đã giữ cổng sạc #{request.AssignedPoint} tại {hub.ShortName} cho {_vehicleCode}: trả xe ở đây là xe vào sạc ngay. Bạn có thể đặt xe mới tại Hub này."
                : $"Đã gửi yêu cầu sạc cho {_vehicleCode}: vị trí #{request.QueuePosition} tại {hub.ShortName}, chờ ~{request.WaitMinutes} phút. Bạn có thể đặt xe mới tại Hub này.";
            Shell.Replace(new ReturnVehicleScreen(App, trip => Shell.ResetTo(new TripSummaryScreen(App, trip, hubId, note)), hubId));
        }
    }
}
