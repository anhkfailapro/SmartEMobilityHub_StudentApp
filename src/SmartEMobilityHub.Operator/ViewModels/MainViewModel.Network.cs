using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>
/// Bản nâng cấp giao diện: điều hướng theo trang (thanh bên), trang Tổng quan mạng lưới
/// và trang Phương tiện &amp; cổng sạc (giám sát trạng thái xe, mức pin, cổng sạc theo Hub).
/// </summary>
public sealed partial class MainViewModel
{
    public const int PageOverview = 0;
    public const int PageIncidents = 1;
    public const int PageAssets = 2;
    public const int PageCharging = 3;
    public const int PageDispatch = 4;
    public const int PageDemo = 5;

    private const string AllHubs = "Tất cả Hub";

    private int _pageIndex = PageOverview;
    private string _assetHubFilter = AllHubs;
    private DeviceFilterOption? _vehicleFilter;
    private string _assetSearchText = "";
    private HubPortGroup? _chargingHubPorts;

    private string _kpiHubsText = "0/0";
    private string _kpiHubsHint = "";
    private string _kpiVehiclesText = "0/0";
    private string _kpiVehiclesHint = "";
    private string _kpiPortsText = "0/0";
    private string _kpiPortsHint = "";
    private string _kpiOpenIncidents = "0";
    private string _networkSummaryText = "";
    private string _assetSummaryText = "";
    private string _atRiskHint = "";

    public ObservableCollection<NavItem> NavItems { get; } = new()
    {
        new NavItem("", "Tổng quan mạng lưới"),
        new NavItem("", "Sự cố & bảo trì"),
        new NavItem("", "Xe & cổng sạc"),
        new NavItem("", "Lịch sạc thông minh"),
        new NavItem("", "Điều phối phương tiện"),
        new NavItem("", "Kịch bản demo & nhật ký")
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

    private Visibility Page(int index) => _pageIndex == index ? Visibility.Visible : Visibility.Collapsed;

    // ---------- Tổng quan ----------
    public ObservableCollection<HubRow> AtRiskHubs { get; } = new();

    public string KpiHubsText { get => _kpiHubsText; private set => SetProperty(ref _kpiHubsText, value); }
    public string KpiHubsHint { get => _kpiHubsHint; private set => SetProperty(ref _kpiHubsHint, value); }
    public string KpiVehiclesText { get => _kpiVehiclesText; private set => SetProperty(ref _kpiVehiclesText, value); }
    public string KpiVehiclesHint { get => _kpiVehiclesHint; private set => SetProperty(ref _kpiVehiclesHint, value); }
    public string KpiPortsText { get => _kpiPortsText; private set => SetProperty(ref _kpiPortsText, value); }
    public string KpiPortsHint { get => _kpiPortsHint; private set => SetProperty(ref _kpiPortsHint, value); }
    public string KpiOpenIncidents { get => _kpiOpenIncidents; private set => SetProperty(ref _kpiOpenIncidents, value); }
    public string NetworkSummaryText { get => _networkSummaryText; private set => SetProperty(ref _networkSummaryText, value); }
    public string AtRiskHint { get => _atRiskHint; private set => SetProperty(ref _atRiskHint, value); }
    public Visibility AtRiskEmptyVisibility => AtRiskHubs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    // ---------- Phương tiện & cổng sạc ----------
    public ObservableCollection<string> AssetHubOptions { get; } = new();
    public ObservableCollection<VehicleRow> VehicleRows { get; } = new();
    public ObservableCollection<HubPortGroup> PortGroups { get; } = new();

    public ObservableCollection<DeviceFilterOption> VehicleFilters { get; } = new()
    {
        new DeviceFilterOption("Tất cả"),
        new DeviceFilterOption("Sẵn sàng", DeviceStatus.Available),
        new DeviceFilterOption("Đặt / đang dùng", DeviceStatus.Reserved, DeviceStatus.InUse, DeviceStatus.Relocating),
        new DeviceFilterOption("Sạc / cần sạc", DeviceStatus.Charging, DeviceStatus.NeedsCharge),
        new DeviceFilterOption("Lỗi / khoá", DeviceStatus.Faulty, DeviceStatus.Locked, DeviceStatus.UnderCheck, DeviceStatus.UnderRepair),
        new DeviceFilterOption("Mất kết nối", DeviceStatus.Offline)
    };

    public DeviceFilterOption SelectedVehicleFilter
    {
        get => _vehicleFilter ??= VehicleFilters[0];
        set
        {
            if (value is null || !SetProperty(ref _vehicleFilter, value)) return;
            RefreshAssets();
        }
    }

    public string AssetHubFilter
    {
        get => _assetHubFilter;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !SetProperty(ref _assetHubFilter, value)) return;
            RefreshAssets();
        }
    }

    public string AssetSearchText
    {
        get => _assetSearchText;
        set
        {
            if (!SetProperty(ref _assetSearchText, value ?? "")) return;
            RefreshAssets();
        }
    }

    public string AssetSummaryText { get => _assetSummaryText; private set => SetProperty(ref _assetSummaryText, value); }
    public Visibility VehicleEmptyVisibility => VehicleRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Lưới cổng sạc của Hub đang chọn ở trang Lịch sạc thông minh.</summary>
    public HubPortGroup? ChargingHubPorts { get => _chargingHubPorts; private set => SetProperty(ref _chargingHubPorts, value); }

    /// <summary>Bấm thẻ Hub ở trang Tổng quan: mở trang Phương tiện &amp; cổng sạc, lọc theo Hub đó.</summary>
    public void OpenHubAssets(string hubId)
    {
        _vehicleFilter = VehicleFilters[0];
        OnPropertyChanged(nameof(SelectedVehicleFilter));
        _assetSearchText = "";
        OnPropertyChanged(nameof(AssetSearchText));
        if (!AssetHubOptions.Contains(hubId)) RefreshNetwork();
        _assetHubFilter = AssetHubOptions.Contains(hubId) ? hubId : AllHubs;
        OnPropertyChanged(nameof(AssetHubFilter));
        RefreshAssets();
        PageIndex = PageAssets;
    }

    private void RefreshNetwork()
    {
        var store = _demo.Store;
        var hubs = store.Hubs;
        var online = hubs.Count(h => h.Status == HubStatus.Online && h.Connection == ConnectionStatus.Online);
        var suspended = hubs.Count(h => h.Status == HubStatus.Suspended);
        KpiHubsText = $"{online}/{hubs.Count}";
        KpiHubsHint = suspended > 0 ? $"{hubs.Count - online - suspended} mất kết nối · {suspended} tạm dừng" : $"{hubs.Count - online} mất kết nối";

        var vehicles = store.Vehicles;
        var available = vehicles.Count(v => v.Status == DeviceStatus.Available);
        var lowBattery = vehicles.Count(v => v.BatteryLevel < 20);
        KpiVehiclesText = $"{available}/{vehicles.Count}";
        KpiVehiclesHint = $"{vehicles.Count(v => v.Status is DeviceStatus.InUse or DeviceStatus.Reserved)} đang dùng/đặt · {lowBattery} pin < 20%";

        var ports = hubs.SelectMany(h => h.ChargingPoints).ToList();
        var freePorts = ports.Count(p => p.Status == DeviceStatus.Available);
        var brokenPorts = ports.Count(p => p.Status is DeviceStatus.Faulty or DeviceStatus.Offline or DeviceStatus.Locked or DeviceStatus.UnderRepair);
        KpiPortsText = $"{freePorts}/{ports.Count}";
        KpiPortsHint = $"{ports.Count(p => p.Status == DeviceStatus.Charging)} đang sạc · {brokenPorts} lỗi/offline";

        var all = _demo.Incidents.GetIncidentQueue();
        var open = all.Count(i => i.Status is not (IncidentStatus.Resolved or IncidentStatus.Closed));
        KpiOpenIncidents = open.ToString();

        var rows = Hubs.ToList();
        AtRiskHubs.Clear();
        foreach (var row in rows.Where(r => r.IsAtRisk).OrderByDescending(r => r.MaxUtilization))
            AtRiskHubs.Add(row);
        OnPropertyChanged(nameof(AtRiskEmptyVisibility));
        AtRiskHint = AtRiskHubs.Count == 0
            ? "Không có Hub nào chạm ngưỡng 80%."
            : $"{AtRiskHubs.Count} Hub từ 80% sức chứa · ngưỡng cảnh báo {_demo.Threshold.Threshold:P0}";

        NetworkSummaryText = $"{hubs.Count} Hub · {vehicles.Count} xe · {ports.Count} cổng sạc · thời gian mô phỏng {_demo.Clock.Now:HH:mm dd/MM}";

        var waiting = store.ChargingRequests.Count(r => r.AssignedPortId is null);
        NavItems[PageOverview].Badge = AtRiskHubs.Count > 0 ? AtRiskHubs.Count.ToString() : "";
        NavItems[PageIncidents].Badge = open > 0 ? open.ToString() : "";
        NavItems[PageAssets].Badge = brokenPorts + vehicles.Count(v => v.Status is DeviceStatus.Faulty or DeviceStatus.Offline) is var bad && bad > 0 ? bad.ToString() : "";
        NavItems[PageCharging].Badge = waiting > 0 ? waiting.ToString() : "";
        NavItems[PageDispatch].Badge = store.DispatchOrders.Count(o => o.IsActive) is var active && active > 0 ? active.ToString() : "";

        var options = new List<string> { AllHubs };
        options.AddRange(hubs.Select(h => h.Id).OrderBy(id => id, StringComparer.Ordinal));
        if (!AssetHubOptions.SequenceEqual(options))
        {
            var keep = _assetHubFilter;
            AssetHubOptions.Clear();
            foreach (var o in options) AssetHubOptions.Add(o);
            _assetHubFilter = options.Contains(keep) ? keep : AllHubs;
            OnPropertyChanged(nameof(AssetHubFilter));
        }

        RefreshAssets();
        RefreshChargingPorts();
    }

    private void RefreshAssets()
    {
        var store = _demo.Store;
        var now = _demo.Clock.Now;
        var hubFilter = _assetHubFilter == AllHubs ? null : _assetHubFilter;
        var query = _assetSearchText.Trim();

        var vehicles = store.Vehicles
            .Where(v => hubFilter is null || v.HubId == hubFilter)
            .Where(v => SelectedVehicleFilter.Matches(v.Status))
            .Where(v => query.Length == 0
                        || v.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (v.Model ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (v.HubId ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(v => v.BatteryLevel)
            .ThenBy(v => v.Code, StringComparer.Ordinal)
            .ToList();

        VehicleRows.Clear();
        foreach (var v in vehicles) VehicleRows.Add(new VehicleRow(v, store.FindHub(v.HubId ?? ""), now));
        OnPropertyChanged(nameof(VehicleEmptyVisibility));

        PortGroups.Clear();
        foreach (var hub in store.Hubs.Where(h => hubFilter is null || h.Id == hubFilter).OrderBy(h => h.Id, StringComparer.Ordinal))
            if (hub.ChargingPoints.Count > 0) PortGroups.Add(new HubPortGroup(hub));

        var scope = store.Vehicles.Where(v => hubFilter is null || v.HubId == hubFilter).ToList();
        AssetSummaryText = $"Hiển thị {vehicles.Count}/{scope.Count} xe · pin trung bình {(scope.Count == 0 ? 0 : scope.Average(v => v.BatteryLevel)):0}% · sắp xếp pin thấp trước";
    }

    private void RefreshChargingPorts()
    {
        var hub = _demo.Store.FindHub(ChargingHubId);
        ChargingHubPorts = hub is null ? null : new HubPortGroup(hub);
    }
}
