using System.Globalization;
using SmartEMobilityHub.Operator.Infrastructure;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>
/// Lệnh cho các nút của 6 trang (DC-52). Trước đây mỗi nút là một handler Click trong MainWindow.xaml.cs;
/// giờ nút bind thẳng vào lệnh, còn code-behind chỉ giữ việc của cửa sổ (đăng xuất, đổi giao diện, mở What-if).
/// Các phương thức nghiệp vụ (LockSelected, AssignSelected…) giữ nguyên để kịch bản chụp màn hình gọi trực tiếp.
/// </summary>
public sealed partial class MainViewModel
{
    private const string ActionFailedTitle = "Không thực hiện được";
    private const string DispatchTitle = "Điều phối";

    /// <summary>Hộp thoại của cửa sổ đang hiển thị ViewModel; null = không hỏi/không báo (chụp màn hình).</summary>
    public IDialogService? Dialogs { get; set; }

    /// <summary>true khi Trung tâm điều hành dùng chung dữ liệu với app Sinh viên (không tự đặt lại được).</summary>
    public bool IsLinked { get; set; }

    /// <summary>Cửa sổ đăng ký để tạo phiên demo mới khi người vận hành đồng ý đặt lại.</summary>
    public Action? ResetRequested { get; set; }

    private RelayCommand? _scanCommand;
    private RelayCommand? _openHubAssetsCommand;
    private RelayCommand? _lockCommand;
    private RelayCommand? _createTaskCommand;
    private RelayCommand? _assignCommand;
    private RelayCommand? _resolveCommand;
    private RelayCommand? _closeIncidentCommand;
    private RelayCommand? _suspendHubCommand;
    private RelayCommand? _resumeHubCommand;
    private RelayCommand? _queueUpCommand;
    private RelayCommand? _queueDownCommand;
    private RelayCommand? _rescheduleCommand;
    private RelayCommand? _applyWeightsCommand;
    private RelayCommand? _resetWeightsCommand;
    private RelayCommand? _addChargingRequestsCommand;
    private RelayCommand? _suggestDispatchCommand;
    private RelayCommand? _createDispatchCommand;
    private RelayCommand? _executeDispatchCommand;
    private RelayCommand? _completeDispatchCommand;
    private RelayCommand? _breakPortCommand;
    private RelayCommand? _disconnectHubCommand;
    private RelayCommand? _makeStaleCommand;
    private RelayCommand? _breakChannelCommand;
    private RelayCommand? _restoreChannelCommand;
    private RelayCommand? _unlockIncidentCommand;
    private RelayCommand? _advanceCommand;
    private RelayCommand? _resetDemoCommand;

    // ---------- Header ----------
    public RelayCommand ScanCommand => _scanCommand ??= new(ScanNow);

    // ---------- Tổng quan ----------
    /// <summary>Tham số: mã Hub của thẻ được bấm.</summary>
    public RelayCommand OpenHubAssetsCommand => _openHubAssetsCommand ??= new(p =>
    {
        if (p is string hubId) OpenHubAssets(hubId);
    });

    // ---------- Sự cố & bảo trì ----------
    public RelayCommand LockCommand => _lockCommand ??= new(() => Run(LockSelected));
    public RelayCommand CreateTaskCommand => _createTaskCommand ??= new(() => Run(CreateTaskSelected));
    public RelayCommand AssignCommand => _assignCommand ??= new(() => Run(AssignSelected));
    public RelayCommand ResolveCommand => _resolveCommand ??= new(() => Run(ResolveSelected));
    public RelayCommand CloseIncidentCommand => _closeIncidentCommand ??= new(() => Run(CloseSelected));
    public RelayCommand ResumeHubCommand => _resumeHubCommand ??= new(() => Run(ResumeSelectedHub));

    public RelayCommand SuspendHubCommand => _suspendHubCommand ??= new(() =>
    {
        if (SelectedIncident is null) return;
        var hubId = SelectedIncident.HubId;
        var confirmed = Dialogs?.Confirm("Xác nhận tạm dừng Hub",
            $"Tạm dừng {hubId}?\nToàn bộ đặt chỗ sắp tới tại Hub này sẽ bị huỷ và người dùng được thông báo.") ?? true;
        if (confirmed) Run(SuspendSelectedHub);
    });

    // ---------- Lịch sạc thông minh ----------
    public RelayCommand QueueUpCommand => _queueUpCommand ??= new(() => Run(() => MoveSelectedQueueRow(-1)));
    public RelayCommand QueueDownCommand => _queueDownCommand ??= new(() => Run(() => MoveSelectedQueueRow(1)));
    public RelayCommand RescheduleCommand => _rescheduleCommand ??= new(RescheduleCharging);
    public RelayCommand ApplyWeightsCommand => _applyWeightsCommand ??= new(() => Run(ApplyPriorityWeights));
    public RelayCommand ResetWeightsCommand => _resetWeightsCommand ??= new(ResetPriorityWeights);
    public RelayCommand AddChargingRequestsCommand => _addChargingRequestsCommand ??= new(AddChargingRequests);

    // ---------- Điều phối ----------
    public RelayCommand SuggestDispatchCommand => _suggestDispatchCommand ??= new(() => Run(SuggestAndCreateDispatch, DispatchTitle));
    public RelayCommand CreateDispatchCommand => _createDispatchCommand ??= new(() => Run(CreateDispatchOrder, DispatchTitle));
    public RelayCommand ExecuteDispatchCommand => _executeDispatchCommand ??= new(() => Run(ExecuteSelectedDispatch, DispatchTitle));
    public RelayCommand CompleteDispatchCommand => _completeDispatchCommand ??= new(() => Run(CompleteSelectedDispatch, DispatchTitle));

    // ---------- Kịch bản demo ----------
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
        if (IsLinked)
        {
            Dialogs?.Inform("Đặt lại demo",
                "Trung tâm điều hành đang dùng chung dữ liệu với app Sinh viên.\nHãy dùng \"Đặt lại toàn bộ demo\" ở Demo panel của app Sinh viên để đặt lại cả hai bên.");
            return;
        }

        var confirmed = Dialogs?.Confirm("Đặt lại demo", "Đặt lại toàn bộ dữ liệu demo? Mọi thay đổi sẽ mất.") ?? true;
        if (confirmed) ResetRequested?.Invoke();
    });

    /// <summary>Chạy một thao tác trả về lỗi (null = thành công) và báo lỗi bằng hộp thoại.</summary>
    private void Run(Func<string?> action, string title = ActionFailedTitle)
    {
        var error = action();
        if (error is not null) Dialogs?.Warn(title, error);
    }
}
