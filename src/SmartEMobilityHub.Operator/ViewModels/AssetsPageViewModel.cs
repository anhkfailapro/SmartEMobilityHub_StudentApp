using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>Trang "Phương tiện &amp; cổng sạc": giám sát trạng thái xe, mức pin, cổng sạc theo Hub.</summary>
public sealed class AssetsPageViewModel : PageViewModel
{
    public const string AllHubs = "Tất cả Hub";

    private string _assetHubFilter = AllHubs;
    private DeviceFilterOption? _vehicleFilter;
    private string _assetSearchText = "";
    private string _assetSummaryText = "";

    public AssetsPageViewModel(DashboardContext shell) : base(shell)
    {
    }

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

    /// <summary>Số xe và cổng sạc đang lỗi / mất kết nối (huy hiệu của trang).</summary>
    public int FaultyCount
    {
        get
        {
            var brokenPorts = Store.Hubs.SelectMany(h => h.ChargingPoints)
                .Count(p => p.Status is DeviceStatus.Faulty or DeviceStatus.Offline or DeviceStatus.Locked or DeviceStatus.UnderRepair);
            return brokenPorts + Store.Vehicles.Count(v => v.Status is DeviceStatus.Faulty or DeviceStatus.Offline);
        }
    }

    /// <summary>Xem xe và cổng sạc của một Hub, bỏ các bộ lọc khác.</summary>
    public void ShowHub(string hubId)
    {
        _vehicleFilter = VehicleFilters[0];
        OnPropertyChanged(nameof(SelectedVehicleFilter));
        _assetSearchText = "";
        OnPropertyChanged(nameof(AssetSearchText));
        if (!AssetHubOptions.Contains(hubId)) RefreshHubOptions();
        _assetHubFilter = AssetHubOptions.Contains(hubId) ? hubId : AllHubs;
        OnPropertyChanged(nameof(AssetHubFilter));
        RefreshAssets();
    }

    public override void Refresh()
    {
        RefreshHubOptions();
        RefreshAssets();
    }

    private void RefreshHubOptions()
    {
        var options = new List<string> { AllHubs };
        options.AddRange(Store.Hubs.Select(h => h.Id).OrderBy(id => id, StringComparer.Ordinal));
        if (AssetHubOptions.SequenceEqual(options)) return;

        var keep = _assetHubFilter;
        AssetHubOptions.Clear();
        foreach (var o in options) AssetHubOptions.Add(o);
        _assetHubFilter = options.Contains(keep) ? keep : AllHubs;
        OnPropertyChanged(nameof(AssetHubFilter));
    }

    private void RefreshAssets()
    {
        var hubFilter = _assetHubFilter == AllHubs ? null : _assetHubFilter;
        var query = _assetSearchText.Trim();

        var vehicles = Store.Vehicles
            .Where(v => hubFilter is null || v.HubId == hubFilter)
            .Where(v => SelectedVehicleFilter.Matches(v.Status))
            .Where(v => query.Length == 0
                        || v.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (v.Model ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (v.HubId ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(v => v.BatteryLevel)
            .ThenBy(v => v.Code, StringComparer.Ordinal)
            .ToList();

        var now = Now;
        VehicleRows.Clear();
        foreach (var v in vehicles) VehicleRows.Add(new VehicleRow(v, Store.FindHub(v.HubId ?? ""), now));
        OnPropertyChanged(nameof(VehicleEmptyVisibility));

        PortGroups.Clear();
        foreach (var hub in Store.Hubs.Where(h => hubFilter is null || h.Id == hubFilter).OrderBy(h => h.Id, StringComparer.Ordinal))
            if (hub.ChargingPoints.Count > 0) PortGroups.Add(new HubPortGroup(hub));

        var scope = Store.Vehicles.Where(v => hubFilter is null || v.HubId == hubFilter).ToList();
        AssetSummaryText = $"Hiển thị {vehicles.Count}/{scope.Count} xe · pin trung bình {(scope.Count == 0 ? 0 : scope.Average(v => v.BatteryLevel)):0}% · sắp xếp pin thấp trước";
    }
}
