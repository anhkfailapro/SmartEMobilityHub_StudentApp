using System.Collections.ObjectModel;
using System.Globalization;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>Trang "Kịch bản demo &amp; nhật ký": các nút bơm sự kiện IoT, mạng cảnh báo, tua giờ, đặt lại demo.</summary>
public sealed class DemoPageViewModel : PageViewModel
{
    private RelayCommand? _breakPortCommand;
    private RelayCommand? _disconnectHubCommand;
    private RelayCommand? _makeStaleCommand;
    private RelayCommand? _breakChannelCommand;
    private RelayCommand? _restoreChannelCommand;
    private RelayCommand? _unlockIncidentCommand;
    private RelayCommand? _advanceCommand;
    private RelayCommand? _resetDemoCommand;

    public DemoPageViewModel(DashboardContext shell) : base(shell)
    {
    }

    private OperatorDemo Demo => Shell.Demo;

    public ObservableCollection<AlertRow> Alerts { get; } = new();
    public ObservableCollection<string> Logs => Demo.Logger.Lines;

    public RelayCommand BreakPortCommand => _breakPortCommand ??= new(BreakPort);
    public RelayCommand DisconnectHubCommand => _disconnectHubCommand ??= new(DisconnectHub);
    public RelayCommand MakeStaleCommand => _makeStaleCommand ??= new(MakeStale);
    public RelayCommand BreakChannelCommand => _breakChannelCommand ??= new(BreakChannel);
    public RelayCommand RestoreChannelCommand => _restoreChannelCommand ??= new(RestoreChannel);
    public RelayCommand UnlockIncidentCommand => _unlockIncidentCommand ??= new(RaiseUnlockIncident);

    /// <summary>Tham số: số phút cần tua (chuỗi "1", "5", "15" từ XAML hoặc số nguyên).</summary>
    public RelayCommand AdvanceCommand => _advanceCommand ??= new(p =>
    {
        var minutes = p switch
        {
            int i => i,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
        if (minutes > 0) Advance(minutes);
    });

    public RelayCommand ResetDemoCommand => _resetDemoCommand ??= new(() =>
    {
        if (Shell.IsLinked)
        {
            Shell.Dialogs?.Inform("Đặt lại demo",
                "Trung tâm điều hành đang dùng chung dữ liệu với app Sinh viên.\nHãy dùng \"Đặt lại toàn bộ demo\" ở Demo panel của app Sinh viên để đặt lại cả hai bên.");
            return;
        }

        var confirmed = Shell.Dialogs?.Confirm("Đặt lại demo", "Đặt lại toàn bộ dữ liệu demo? Mọi thay đổi sẽ mất.") ?? true;
        if (confirmed) Shell.ResetRequested?.Invoke();
    });

    public override void Refresh()
    {
        Alerts.Clear();
        foreach (var alert in Shell.LatestAlerts()) Alerts.Add(new AlertRow(alert));
    }

    public void Advance(int minutes)
    {
        Demo.Advance(minutes);
        Note($"Đã tua +{minutes} phút. Bấm “Quét ngay” để áp dụng chu kỳ.");
        RefreshAll();
    }

    public void BreakPort()
    {
        Demo.BreakDemoPort();
        Note($"Đã bơm lỗi cổng {OperatorDemo.DemoBrokenPointId} · sinh viên/cảm biến đã tạo sự cố.");
        RefreshAll();
    }

    public void DisconnectHub()
    {
        Demo.DisconnectDemoHub();
        Note($"Đã ngắt cảm biến {OperatorDemo.DemoDisconnectedHubId} · cả 4 cổng chuyển OFFLINE.");
        RefreshAll();
    }

    public void MakeStale()
    {
        Demo.MakeDemoHubStale();
        Note($"Dữ liệu {OperatorDemo.DemoStaleHubId} đã cũ 14 phút · cảnh báo STALE_DATA đã phát.");
        RefreshAll();
    }

    public void BreakChannel()
    {
        var pending = Demo.BreakAlertChannel();
        Note($"Mạng cảnh báo đang lỗi · {pending} cảnh báo nằm trong hàng đợi gửi lại.");
        RefreshAll();
    }

    public void RestoreChannel()
    {
        var sent = Demo.RestoreAlertChannel();
        Note($"Đã khôi phục mạng · gửi lại thành công {sent} cảnh báo.");
        RefreshAll();
    }

    public void RaiseUnlockIncident()
    {
        var record = Demo.RaiseUnlockTimeout();
        Note($"Đã tạo {record.IncidentId} · xe XE-121 giữ RESERVED, đơn R-103 bị huỷ.");
        RefreshAll();
    }
}
