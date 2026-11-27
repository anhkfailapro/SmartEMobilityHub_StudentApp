using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Infrastructure;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>
/// Trạng thái dùng chung giữa các trang của Trung tâm điều hành: phiên mạng lưới, dòng trạng thái cuối cửa sổ,
/// chế độ tự quét/tăng tốc, hộp thoại và các yêu cầu gửi lên khung điều hướng (vẽ lại, mở trang Xe &amp; cổng sạc).
/// Các ViewModel trang chỉ biết lớp này, không biết <see cref="MainViewModel"/> hay ViewModel trang khác.
/// </summary>
public sealed class DashboardContext : ObservableObject
{
    public const string ActionFailedTitle = "Không thực hiện được";

    private string _lastAction = "Sẵn sàng — bấm “Quét ngay” hoặc để tự động quét mỗi 30 giây.";
    private bool _autoScan = true;
    private bool _speedUp;

    public DashboardContext(OperatorDemo demo)
    {
        Demo = demo;
    }

    /// <summary>Các lệnh kịch bản demo và nhật ký.</summary>
    public OperatorDemo Demo { get; }

    public NetworkSession Session => Demo.Session;

    /// <summary>Hộp thoại của cửa sổ đang hiển thị; null = không hỏi/không báo (chụp màn hình, test).</summary>
    public IDialogService? Dialogs { get; set; }

    /// <summary>true khi Trung tâm điều hành dùng chung dữ liệu với app Sinh viên (không tự đặt lại được).</summary>
    public bool IsLinked { get; set; }

    /// <summary>Cửa sổ đăng ký để tạo phiên demo mới khi người vận hành đồng ý đặt lại.</summary>
    public Action? ResetRequested { get; set; }

    /// <summary>Có thao tác làm đổi dữ liệu: khung điều hướng vẽ lại mọi trang và huy hiệu.</summary>
    public event Action? RefreshRequested;

    /// <summary>Yêu cầu mở trang Xe &amp; cổng sạc, lọc theo một Hub (bấm thẻ Hub ở Tổng quan).</summary>
    public event Action<string>? HubAssetsRequested;

    public string LastActionText
    {
        get => _lastAction;
        private set => SetProperty(ref _lastAction, value);
    }

    public bool AutoScan { get => _autoScan; set => SetProperty(ref _autoScan, value); }
    public bool SpeedUp { get => _speedUp; set => SetProperty(ref _speedUp, value); }

    public void Note(string message) => LastActionText = message;

    public void RequestRefresh() => RefreshRequested?.Invoke();

    public void RequestHubAssets(string hubId) => HubAssetsRequested?.Invoke(hubId);

    /// <summary>Chạy một thao tác trả về lỗi (null = thành công) và báo lỗi bằng hộp thoại.</summary>
    public void Run(Func<string?> action, string title = ActionFailedTitle)
    {
        var error = action();
        if (error is not null) Dialogs?.Warn(title, error);
    }

    /// <summary>Cảnh báo mới nhất: đã gửi và đang chờ gửi lại, mỗi cảnh báo một lần.</summary>
    public IReadOnlyList<Alert> LatestAlerts(int take = 6) =>
        Session.Notification.OperatorAlerts
            .Concat(Session.RetryQueue.Pending)
            .GroupBy(a => a.AlertId)
            .Select(g => g.First())
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToList();

    /// <summary>"sẵn sàng/tổng" kỹ thuật viên.</summary>
    public string TechnicianSummary()
    {
        var technicians = Session.Store.Technicians;
        return $"{technicians.Count(t => t.IsAvailable())}/{technicians.Count}";
    }
}
