using System.Collections.ObjectModel;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>Trang "Điều phối phương tiện": tải từng Hub, gợi ý và vòng đời lệnh điều phối.</summary>
public sealed class DispatchPageViewModel : PageViewModel
{
    private const string DispatchTitle = "Điều phối";

    /// <summary>Ngưỡng Hub nhận khi không có Hub nào thiếu xe rõ rệt (≤ 35%).</summary>
    private const double RelaxedReceiverThreshold = 0.5;

    private string _sourceHubId = "";
    private string _targetHubId = "";
    private string _dispatchVehicleCountText = "2";
    private string _dispatchStaffId = "NV-DIEUPHOI-01";
    private DispatchOrderRow? _selectedDispatchOrder;

    private RelayCommand? _suggestDispatchCommand;
    private RelayCommand? _createDispatchCommand;
    private RelayCommand? _executeDispatchCommand;
    private RelayCommand? _completeDispatchCommand;

    public DispatchPageViewModel(DashboardContext shell) : base(shell)
    {
    }

    public ObservableCollection<HubRow> Hubs { get; } = new();
    public ObservableCollection<string> HubIds { get; } = new();
    public ObservableCollection<DispatchOrderRow> DispatchOrders { get; } = new();

    public string SourceHubId { get => _sourceHubId; set => SetProperty(ref _sourceHubId, value); }
    public string TargetHubId { get => _targetHubId; set => SetProperty(ref _targetHubId, value); }
    public string DispatchVehicleCountText { get => _dispatchVehicleCountText; set => SetProperty(ref _dispatchVehicleCountText, value); }
    public string DispatchStaffId { get => _dispatchStaffId; set => SetProperty(ref _dispatchStaffId, value); }

    public DispatchOrderRow? SelectedDispatchOrder
    {
        get => _selectedDispatchOrder;
        set
        {
            if (ReferenceEquals(_selectedDispatchOrder, value)) return;
            _selectedDispatchOrder = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanExecuteDispatch));
            OnPropertyChanged(nameof(CanCompleteDispatch));
        }
    }

    public bool CanExecuteDispatch => _selectedDispatchOrder?.StatusRaw == DispatchOrderStatus.Pending;
    public bool CanCompleteDispatch => _selectedDispatchOrder?.StatusRaw == DispatchOrderStatus.InProgress;

    // ---------- Lệnh ----------
    public RelayCommand SuggestDispatchCommand => _suggestDispatchCommand ??= new(() => Run(SuggestAndCreateDispatch, DispatchTitle));
    public RelayCommand CreateDispatchCommand => _createDispatchCommand ??= new(() => Run(CreateDispatchOrder, DispatchTitle));
    public RelayCommand ExecuteDispatchCommand => _executeDispatchCommand ??= new(() => Run(ExecuteSelectedDispatch, DispatchTitle));
    public RelayCommand CompleteDispatchCommand => _completeDispatchCommand ??= new(() => Run(CompleteSelectedDispatch, DispatchTitle));

    public override void Refresh()
    {
        var currentIds = Store.Hubs.Select(h => h.Id).ToList();
        if (!HubIds.SequenceEqual(currentIds))
        {
            HubIds.Clear();
            foreach (var id in currentIds) HubIds.Add(id);
        }

        if (string.IsNullOrWhiteSpace(SourceHubId) && HubIds.Count > 0)
            SourceHubId = HubIds[0];
        if (string.IsNullOrWhiteSpace(TargetHubId) && HubIds.Count > 1)
            TargetHubId = HubIds[1];

        Hubs.Clear();
        foreach (var row in HubRow.ForNetwork(Store)) Hubs.Add(row);

        var selectedOrderId = _selectedDispatchOrder?.OrderId;
        DispatchOrders.Clear();
        foreach (var order in Store.DispatchOrders.OrderByDescending(o => o.CreatedAt))
            DispatchOrders.Add(new DispatchOrderRow(order));

        SelectedDispatchOrder = DispatchOrders.FirstOrDefault(o => o.OrderId == selectedOrderId)
                                ?? DispatchOrders.FirstOrDefault();

        OnPropertyChanged(nameof(CanExecuteDispatch));
        OnPropertyChanged(nameof(CanCompleteDispatch));
    }

    public string? SuggestAndCreateDispatch()
    {
        var redistributor = Session.Redistributor;
        var suggestions = redistributor.GenerateDispatchSuggestions(Store.Hubs);
        var relaxed = false;
        if (suggestions.Count == 0)
        {
            // Có Hub dư xe nhưng không Hub nào ≤ 35%: nới ngưỡng Hub nhận tới 50% để vẫn hạ tải được Hub đang quá tải.
            var low = redistributor.LowLoadThreshold;
            try
            {
                redistributor.LowLoadThreshold = RelaxedReceiverThreshold;
                suggestions = redistributor.GenerateDispatchSuggestions(Store.Hubs);
                relaxed = suggestions.Count > 0;
            }
            finally
            {
                redistributor.LowLoadThreshold = low;
            }
        }

        var first = suggestions.FirstOrDefault();
        if (first is null)
            return "Hiện chưa phát hiện cặp Hub lệch tải rõ rệt để gợi ý tự động.";

        SourceHubId = first.SourceHubId;
        TargetHubId = first.TargetHubId;
        DispatchVehicleCountText = Math.Max(1, first.VehicleIds.Count).ToString();

        var error = CreateDispatchOrder();
        if (error is null && relaxed)
            Note(Shell.LastActionText + $" Không có Hub ≤ 35% nên đã chọn Hub nhận còn dưới {RelaxedReceiverThreshold:P0}.");
        return error;
    }

    public string? CreateDispatchOrder()
    {
        if (!int.TryParse(DispatchVehicleCountText.Trim(), out var count) || count <= 0)
            return "Số lượng xe điều phối phải là số nguyên dương.";

        var sourceHub = Store.FindHub(SourceHubId);
        var vehicleCodes = new List<string>();

        if (sourceHub is not null)
        {
            vehicleCodes.AddRange(Store.Vehicles
                .Where(v => v.HubId == sourceHub.Id && v.Status == DeviceStatus.Available)
                .Where(v => !Store.DispatchOrders.Any(o => o.IsActive && o.VehicleIds.Contains(v.Code)))
                .Take(count)
                .Select(v => v.Code));
        }

        // Chỉ điều phối xe thật đang sẵn sàng; không tự sinh mã xe giả để bù số lượng.
        if (vehicleCodes.Count == 0)
            return $"{SourceHubId} không còn xe sẵn sàng để điều phối.";
        if (vehicleCodes.Count < count)
            return $"{SourceHubId} chỉ còn {vehicleCodes.Count} xe sẵn sàng (yêu cầu {count}).";

        try
        {
            var order = Session.Redistributor.CreateOrder(
                SourceHubId,
                TargetHubId,
                vehicleCodes,
                DispatchStaffId,
                "Tạo lệnh điều phối trên Operator Dashboard");

            Note($"Đã tạo lệnh {order.OrderId}: {order.SourceHubId} → {order.TargetHubId} ({order.VehicleIds.Count} xe).");
            RefreshAll();
            return null;
        }
        catch (Exception ex) when (ex is IllegalArgumentException or HubNotFoundException)
        {
            return ex.Message;
        }
    }

    public string? ExecuteSelectedDispatch()
    {
        if (_selectedDispatchOrder is null) return "Hãy chọn một lệnh điều phối.";
        if (!Session.Redistributor.ExecuteRedistribution(_selectedDispatchOrder.OrderId))
            return "Chỉ có thể bắt đầu điều phối khi lệnh ở trạng thái Chờ xuất phát.";

        Note($"Đang thực thi lệnh {_selectedDispatchOrder.OrderId}.");
        RefreshAll();
        return null;
    }

    public string? CompleteSelectedDispatch()
    {
        if (_selectedDispatchOrder is null) return "Hãy chọn một lệnh điều phối.";
        if (!Session.Redistributor.CompleteOrder(_selectedDispatchOrder.OrderId))
            return "Chỉ có thể hoàn tất khi lệnh đang ở trạng thái Đang điều phối.";

        Note($"Đã hoàn tất lệnh {_selectedDispatchOrder.OrderId} và cập nhật tải Hub đích.");
        RefreshAll();
        return null;
    }
}
