using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>
/// Khung điều hướng của Trung tâm điều hành: thanh bên (trang đang xem, huy hiệu), header (đồng hồ, chu kỳ quét)
/// và nhịp 1 giây. Nội dung từng trang nằm ở ViewModel riêng (<see cref="OverviewPageViewModel"/>,
/// <see cref="IncidentsPageViewModel"/>, <see cref="AssetsPageViewModel"/>, <see cref="ChargingPageViewModel"/>,
/// <see cref="DispatchPageViewModel"/>, <see cref="DemoPageViewModel"/>); các trang chỉ liên lạc qua
/// <see cref="DashboardContext"/>.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    public const int PageOverview = 0;
    public const int PageIncidents = 1;
    public const int PageAssets = 2;
    public const int PageCharging = 3;
    public const int PageDispatch = 4;
    public const int PageDemo = 5;

    /// <summary>Chu kỳ quét tự động (giây theo đồng hồ mô phỏng).</summary>
    private const int ScanPeriodSeconds = 30;

    private readonly PageViewModel[] _pages;
    private int _pageIndex = PageOverview;
    private string _clockText = "";
    private string _scanText = "";
    private RelayCommand? _scanCommand;

    public MainViewModel(OperatorDemo demo, IDialogService? dialogs = null)
    {
        Shell = new DashboardContext(demo) { Dialogs = dialogs };
        Overview = new OverviewPageViewModel(Shell);
        Incidents = new IncidentsPageViewModel(Shell);
        Assets = new AssetsPageViewModel(Shell);
        Charging = new ChargingPageViewModel(Shell);
        Dispatch = new DispatchPageViewModel(Shell);
        Demo = new DemoPageViewModel(Shell);
        _pages = new PageViewModel[] { Overview, Incidents, Assets, Charging, Dispatch, Demo };

        Shell.RefreshRequested += Refresh;
        Shell.HubAssetsRequested += OpenHubAssets;
        Refresh();
    }

    /// <summary>Trạng thái dùng chung (dòng trạng thái, tự quét, hộp thoại).</summary>
    public DashboardContext Shell { get; }

    public OverviewPageViewModel Overview { get; }
    public IncidentsPageViewModel Incidents { get; }
    public AssetsPageViewModel Assets { get; }
    public ChargingPageViewModel Charging { get; }
    public DispatchPageViewModel Dispatch { get; }
    public DemoPageViewModel Demo { get; }

    public ObservableCollection<NavItem> NavItems { get; } = new()
    {
        new NavItem("", "Tổng quan mạng lưới"),
        new NavItem("", "Sự cố & bảo trì"),
        new NavItem("", "Xe & cổng sạc"),
        new NavItem("", "Lịch sạc thông minh"),
        new NavItem("", "Điều phối phương tiện"),
        new NavItem("", "Kịch bản demo & nhật ký")
    };

    public int PageIndex
    {
        get => _pageIndex;
        set
        {
            // ListBox đặt -1 khi danh sách thay đổi: giữ nguyên trang đang xem.
            if (value < 0 || value >= NavItems.Count || !SetProperty(ref _pageIndex, value)) return;
            OnPropertyChanged(nameof(OverviewVisibility));
            OnPropertyChanged(nameof(IncidentsVisibility));
            OnPropertyChanged(nameof(AssetsVisibility));
            OnPropertyChanged(nameof(ChargingVisibility));
            OnPropertyChanged(nameof(DispatchVisibility));
            OnPropertyChanged(nameof(DemoVisibility));
        }
    }

    public Visibility OverviewVisibility => Page(PageOverview);
    public Visibility IncidentsVisibility => Page(PageIncidents);
    public Visibility AssetsVisibility => Page(PageAssets);
    public Visibility ChargingVisibility => Page(PageCharging);
    public Visibility DispatchVisibility => Page(PageDispatch);
    public Visibility DemoVisibility => Page(PageDemo);

    public string ClockText { get => _clockText; private set => SetProperty(ref _clockText, value); }
    public string ScanText { get => _scanText; private set => SetProperty(ref _scanText, value); }

    public RelayCommand ScanCommand => _scanCommand ??= new(ScanNow);

    /// <summary>Nhịp UI 1 giây: thời gian mô phỏng trôi, tự quét mỗi 30 giây.</summary>
    public void Tick()
    {
        var session = Shell.Session;
        // Đồng hồ mô phỏng tự trôi theo giờ thật (DC-54); chế độ ×10 tua thêm 9 giây mỗi giây.
        if (Shell.SpeedUp) session.Clock.AdvanceSeconds(9);

        var lastTick = session.Timer.LastTickAt;
        var due = lastTick is null || (session.Clock.Now - lastTick.Value).TotalSeconds >= ScanPeriodSeconds;

        if (Shell.AutoScan && due)
        {
            session.Scan();
            Refresh();
            return;
        }

        UpdateHeader();
    }

    public void ScanNow()
    {
        Shell.Session.Scan();
        Shell.Note("Đã chạy chu kỳ quét 6 bước.");
        Refresh();
    }

    /// <summary>Làm mới mọi trang, huy hiệu thanh bên và header.</summary>
    public void Refresh()
    {
        foreach (var page in _pages) page.Refresh();
        UpdateBadges();
        UpdateHeader();
    }

    /// <summary>Mở trang Xe &amp; cổng sạc, lọc theo một Hub.</summary>
    public void OpenHubAssets(string hubId)
    {
        Assets.ShowHub(hubId);
        PageIndex = PageAssets;
    }

    private Visibility Page(int index) => _pageIndex == index ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateBadges()
    {
        var store = Shell.Session.Store;
        var waiting = store.ChargingRequests.Count(r => r.AssignedPortId is null);
        var activeOrders = store.DispatchOrders.Count(o => o.IsActive);

        NavItems[PageOverview].Badge = Badge(Overview.AtRiskHubs.Count);
        NavItems[PageIncidents].Badge = Badge(Overview.OpenIncidentCount);
        NavItems[PageAssets].Badge = Badge(Assets.FaultyCount);
        NavItems[PageCharging].Badge = Badge(waiting);
        NavItems[PageDispatch].Badge = Badge(activeOrders);
    }

    private static string Badge(int count) => count > 0 ? count.ToString() : "";

    private void UpdateHeader()
    {
        var session = Shell.Session;
        ClockText = session.Clock.Now.ToString("HH:mm:ss");
        ScanText = session.Timer.LastTickAt is null
            ? "Chưa quét · chu kỳ 30 giây"
            : $"Cập nhật {session.Timer.GetSecondsSinceLastTick()} giây trước · chu kỳ 30 giây";
    }
}
