using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>Trang "Tổng quan mạng lưới": KPI toàn mạng, thẻ từng Hub, Hub có nguy cơ quá tải, cảnh báo gần đây.</summary>
public sealed class OverviewPageViewModel : PageViewModel
{
    private string _kpiHubsText = "0/0";
    private string _kpiHubsHint = "";
    private string _kpiVehiclesText = "0/0";
    private string _kpiVehiclesHint = "";
    private string _kpiPortsText = "0/0";
    private string _kpiPortsHint = "";
    private string _kpiOpenIncidents = "0";
    private string _kpiNew = "0";
    private string _kpiRepair = "0";
    private string _overloadText = "0";
    private string _techText = "0/0";
    private string _alertCountText = "0";
    private string _networkSummaryText = "";
    private string _atRiskHint = "";
    private RelayCommand? _openHubAssetsCommand;

    public OverviewPageViewModel(DashboardContext shell) : base(shell)
    {
    }

    public ObservableCollection<HubRow> Hubs { get; } = new();
    public ObservableCollection<HubRow> AtRiskHubs { get; } = new();
    public ObservableCollection<AlertRow> Alerts { get; } = new();

    public string KpiHubsText { get => _kpiHubsText; private set => SetProperty(ref _kpiHubsText, value); }
    public string KpiHubsHint { get => _kpiHubsHint; private set => SetProperty(ref _kpiHubsHint, value); }
    public string KpiVehiclesText { get => _kpiVehiclesText; private set => SetProperty(ref _kpiVehiclesText, value); }
    public string KpiVehiclesHint { get => _kpiVehiclesHint; private set => SetProperty(ref _kpiVehiclesHint, value); }
    public string KpiPortsText { get => _kpiPortsText; private set => SetProperty(ref _kpiPortsText, value); }
    public string KpiPortsHint { get => _kpiPortsHint; private set => SetProperty(ref _kpiPortsHint, value); }
    public string KpiOpenIncidents { get => _kpiOpenIncidents; private set => SetProperty(ref _kpiOpenIncidents, value); }
    public string KpiNew { get => _kpiNew; private set => SetProperty(ref _kpiNew, value); }
    public string KpiRepair { get => _kpiRepair; private set => SetProperty(ref _kpiRepair, value); }
    public string OverloadText { get => _overloadText; private set => SetProperty(ref _overloadText, value); }
    public string TechText { get => _techText; private set => SetProperty(ref _techText, value); }
    public string AlertCountText { get => _alertCountText; private set => SetProperty(ref _alertCountText, value); }
    public string NetworkSummaryText { get => _networkSummaryText; private set => SetProperty(ref _networkSummaryText, value); }
    public string AtRiskHint { get => _atRiskHint; private set => SetProperty(ref _atRiskHint, value); }
    public Visibility AtRiskEmptyVisibility => AtRiskHubs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Số sự cố chưa xử lý xong (huy hiệu của trang Sự cố).</summary>
    public int OpenIncidentCount { get; private set; }

    /// <summary>Bấm thẻ Hub: mở trang Xe &amp; cổng sạc lọc theo Hub đó. Tham số: mã Hub.</summary>
    public RelayCommand OpenHubAssetsCommand => _openHubAssetsCommand ??= new(p =>
    {
        if (p is string hubId) Shell.RequestHubAssets(hubId);
    });

    public override void Refresh()
    {
        var hubs = Store.Hubs;
        var online = hubs.Count(h => h.Status == HubStatus.Online && h.Connection == ConnectionStatus.Online);
        var suspended = hubs.Count(h => h.Status == HubStatus.Suspended);
        KpiHubsText = $"{online}/{hubs.Count}";
        KpiHubsHint = suspended > 0 ? $"{hubs.Count - online - suspended} mất kết nối · {suspended} tạm dừng" : $"{hubs.Count - online} mất kết nối";

        var vehicles = Store.Vehicles;
        var available = vehicles.Count(v => v.Status == DeviceStatus.Available);
        var lowBattery = vehicles.Count(v => v.BatteryLevel < 20);
        KpiVehiclesText = $"{available}/{vehicles.Count}";
        KpiVehiclesHint = $"{vehicles.Count(v => v.Status is DeviceStatus.InUse or DeviceStatus.Reserved)} đang dùng/đặt · {lowBattery} pin < 20%";

        var ports = hubs.SelectMany(h => h.ChargingPoints).ToList();
        var freePorts = ports.Count(p => p.Status == DeviceStatus.Available);
        var brokenPorts = ports.Count(p => p.Status is DeviceStatus.Faulty or DeviceStatus.Offline or DeviceStatus.Locked or DeviceStatus.UnderRepair);
        KpiPortsText = $"{freePorts}/{ports.Count}";
        KpiPortsHint = $"{ports.Count(p => p.Status == DeviceStatus.Charging)} đang sạc · {brokenPorts} lỗi/offline";

        var incidents = Session.Incidents.GetIncidentQueue();
        OpenIncidentCount = incidents.Count(i => i.Status is not (IncidentStatus.Resolved or IncidentStatus.Closed));
        KpiOpenIncidents = OpenIncidentCount.ToString();
        KpiNew = incidents.Count(i => i.Status == IncidentStatus.New).ToString();
        KpiRepair = incidents.Count(i => i.Status == IncidentStatus.InRepair).ToString();

        OverloadText = hubs.Count(h =>
            h.Status == HubStatus.Online &&
            h.Connection == ConnectionStatus.Online &&
            h.MaxUtilization >= Session.Threshold.Threshold).ToString();
        TechText = Shell.TechnicianSummary();

        Hubs.Clear();
        foreach (var row in HubRow.ForNetwork(Store)) Hubs.Add(row);

        AtRiskHubs.Clear();
        foreach (var row in Hubs.Where(r => r.IsAtRisk).OrderByDescending(r => r.MaxUtilization))
            AtRiskHubs.Add(row);
        OnPropertyChanged(nameof(AtRiskEmptyVisibility));
        AtRiskHint = AtRiskHubs.Count == 0
            ? "Không có Hub nào chạm ngưỡng 80%."
            : $"{AtRiskHubs.Count} Hub từ 80% sức chứa · ngưỡng cảnh báo {Session.Threshold.Threshold:P0}";

        var alerts = Shell.LatestAlerts();
        Alerts.Clear();
        foreach (var alert in alerts) Alerts.Add(new AlertRow(alert));
        AlertCountText = alerts.Count.ToString();

        NetworkSummaryText = $"{hubs.Count} Hub · {vehicles.Count} xe · {ports.Count} cổng sạc · thời gian mô phỏng {Now:HH:mm dd/MM}";
    }
}
