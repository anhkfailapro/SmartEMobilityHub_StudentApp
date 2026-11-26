using System.Windows;
using System.Windows.Media;
using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Operator.Models;

/// <summary>Nhãn và màu cho trạng thái xe / cổng sạc (DeviceStatus) trên Trung tâm điều hành.</summary>
internal static class DeviceTexts
{
    public static string Vehicle(DeviceStatus status) => status switch
    {
        DeviceStatus.Available => "Sẵn sàng",
        DeviceStatus.Reserved => "Đã đặt",
        DeviceStatus.InUse => "Đang dùng",
        DeviceStatus.Charging => "Đang sạc",
        DeviceStatus.NeedsCharge => "Cần sạc",
        DeviceStatus.UnderCheck => "Đang kiểm tra",
        DeviceStatus.Locked => "Đã khoá",
        DeviceStatus.UnderRepair => "Đang sửa",
        DeviceStatus.Faulty => "Lỗi",
        DeviceStatus.Offline => "Mất kết nối",
        _ => "Đang điều chuyển"
    };

    public static string Port(DeviceStatus status) => status switch
    {
        DeviceStatus.Available => "Trống",
        DeviceStatus.Reserved => "Đã giữ",
        DeviceStatus.InUse => "Đã cấp",
        DeviceStatus.Charging => "Đang sạc",
        _ => Vehicle(status)
    };

    public static (Brush Background, Brush Foreground) Tone(DeviceStatus status) => status switch
    {
        DeviceStatus.Available => Texts.Tone("#D1FAE5", "#047857", "#6EE7B7"),
        DeviceStatus.Reserved => Texts.Tone("#FEF3C7", "#B45309", "#FCD34D"),
        DeviceStatus.InUse => Texts.Tone("#E0F2FE", "#0369A1", "#7DD3FC"),
        DeviceStatus.Relocating => Texts.Tone("#DBEAFE", "#1D4ED8", "#93C5FD"),
        DeviceStatus.Charging => Texts.Tone("#EDE9FE", "#6D28D9", "#C4B5FD"),
        DeviceStatus.NeedsCharge => Texts.Tone("#FFEDD5", "#C2410C", "#FDBA74"),
        DeviceStatus.UnderCheck => Texts.Tone("#E0F2FE", "#0369A1", "#7DD3FC"),
        DeviceStatus.UnderRepair => Texts.Tone("#EDE9FE", "#6D28D9", "#C4B5FD"),
        DeviceStatus.Locked or DeviceStatus.Faulty => Texts.Tone("#FFE4E6", "#BE123C", "#FDA4AF"),
        _ => Texts.Tone("#F1F5F9", "#475569", "#94A3B8")
    };

    /// <summary>Màu theo mức sử dụng: &lt; 80% xanh, 80–89% cam, ≥ 90% đỏ (ngưỡng quá tải).</summary>
    public static Brush Load(double ratio) => Texts.Brush(ratio >= 0.9 ? "#DC2626" : ratio >= 0.8 ? "#EA580C" : "#2563EB");

    public static Brush Battery(int level) => Texts.Brush(level > 50 ? "#10B981" : level >= 20 ? "#F59E0B" : "#EF4444");
}

/// <summary>Thẻ Hub ở trang Tổng quan / Điều phối: chỗ đỗ, sạc, xe sẵn sàng, mức cân bằng.</summary>
public sealed class HubRow
{
    public string HubId { get; }
    public string Name { get; }
    public string FullName { get; }
    public string StatusText { get; }
    public Brush StatusBrush { get; }
    public Brush StatusBackground { get; }
    public string LoadText { get; }
    public string UtilizationText { get; }
    public Brush UtilizationBrush { get; }
    public string FreeSlotsText { get; }
    public string FreeChargersText { get; }
    public string ImbalanceTag { get; }
    public Brush ImbalanceBrush { get; }
    public Brush ImbalanceBackground { get; }

    public double ParkingPercent { get; }
    public double ChargingPercent { get; }
    public string ParkingText { get; }
    public string ChargingText { get; }
    public Brush ParkingBrush { get; }
    public Brush ChargingBrush { get; }
    public string VehiclesText { get; }
    public string AvailableVehiclesText { get; }
    public Brush AccentBrush { get; }
    public bool IsAtRisk { get; }
    public double MaxUtilization { get; }
    public string RiskText { get; }

    public HubRow(MobilityHub hub, IReadOnlyCollection<Vehicle>? vehicles = null)
    {
        HubId = hub.Id;
        Name = string.IsNullOrWhiteSpace(hub.ShortName) ? hub.Name : $"{hub.Id} · {hub.ShortName}";
        FullName = string.IsNullOrWhiteSpace(hub.Name) ? hub.Id : hub.Name;

        var isOnline = hub.Status == HubStatus.Online && hub.Connection == ConnectionStatus.Online;
        StatusText = hub.Status == HubStatus.Suspended
            ? "Tạm dừng"
            : isOnline ? "Hoạt động" : "Mất kết nối";
        (StatusBackground, StatusBrush) = hub.Status == HubStatus.Suspended
            ? Texts.Tone("#FFE4E6", "#BE123C", "#FDA4AF")
            : isOnline ? Texts.Tone("#D1FAE5", "#047857", "#6EE7B7") : Texts.Tone("#FEF3C7", "#B45309", "#FCD34D");

        LoadText = $"{hub.OccupiedParkingSlots}/{hub.ParkingCapacity} chỗ";
        MaxUtilization = hub.MaxUtilization;
        var utilPct = hub.MaxUtilization * 100.0;
        UtilizationText = $"{utilPct:0}%";
        UtilizationBrush = DeviceTexts.Load(hub.MaxUtilization);

        FreeSlotsText = $"Trống {hub.FreeParkingSlots} chỗ";
        var freeChargers = hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Available);
        FreeChargersText = $"{freeChargers}/{hub.UsableChargingPorts} sạc trống";

        ParkingPercent = Math.Clamp(hub.ParkingUtilization * 100, 0, 100);
        ChargingPercent = Math.Clamp(hub.ChargingUtilization * 100, 0, 100);
        ParkingText = $"{hub.OccupiedParkingSlots}/{hub.ParkingCapacity} · {hub.ParkingUtilization * 100:0}%";
        var charging = hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Charging);
        ChargingText = $"{charging}/{hub.UsableChargingPorts} · {hub.ChargingUtilization * 100:0}%";
        ParkingBrush = DeviceTexts.Load(hub.ParkingUtilization);
        ChargingBrush = hub.ChargingUtilization >= 0.9 ? Texts.Brush("#DC2626") : Texts.Brush("#8B5CF6");

        var list = vehicles ?? Array.Empty<Vehicle>();
        var available = list.Count(v => v.Status == DeviceStatus.Available);
        VehiclesText = $"{available}/{list.Count} xe sẵn sàng";
        AvailableVehiclesText = available.ToString();

        IsAtRisk = isOnline && hub.MaxUtilization >= 0.8;
        RiskText = hub.MaxUtilization >= 0.9 ? "Quá tải" : hub.MaxUtilization >= 0.8 ? "Sắp đầy" : "";
        AccentBrush = !isOnline
            ? Texts.Brush("#F59E0B")
            : hub.MaxUtilization >= 0.9
                ? Texts.Brush("#F87171")
                : (Brush)Application.Current.TryFindResource("Border") ?? Texts.Brush("#E2E8F0");

        ImbalanceTag = hub.ParkingUtilization >= 0.85
            ? "Dư xe / Cao"
            : hub.ParkingUtilization <= 0.35
                ? "Thiếu xe"
                : "Cân bằng";
        (ImbalanceBackground, ImbalanceBrush) = hub.ParkingUtilization >= 0.85
            ? Texts.Tone("#FFE4E6", "#BE123C", "#FDA4AF")
            : hub.ParkingUtilization <= 0.35
                ? Texts.Tone("#FEF3C7", "#B45309", "#FCD34D")
                : Texts.Tone("#D1FAE5", "#047857", "#6EE7B7");
    }
}

/// <summary>Một dòng trong bảng giám sát phương tiện.</summary>
public sealed class VehicleRow
{
    public string Code { get; }
    public string TypeText { get; }
    public string HubText { get; }
    public string SlotText { get; }
    public double Battery { get; }
    public string BatteryText { get; }
    public Brush BatteryBrush { get; }
    public string StatusText { get; }
    public Brush StatusBackground { get; }
    public Brush StatusForeground { get; }
    public string UpdatedText { get; }
    public Brush UpdatedBrush { get; }
    public string ConnectionGlyph { get; }
    public Brush ConnectionBrush { get; }
    public string ConnectionText { get; }

    public VehicleRow(Vehicle vehicle, MobilityHub? hub, DateTime now)
    {
        Code = vehicle.Code;
        TypeText = vehicle.Type == VehicleType.EBike ? "Xe đạp điện" : "Xe máy điện";
        if (!string.IsNullOrWhiteSpace(vehicle.Model)) TypeText += $" · {vehicle.Model}";
        HubText = hub is null ? (vehicle.HubId ?? "Đang di chuyển") : $"{hub.Id} · {hub.ShortName}";
        SlotText = string.IsNullOrWhiteSpace(vehicle.Slot) ? "—" : vehicle.Slot;
        Battery = Math.Clamp(vehicle.BatteryLevel, 0, 100);
        BatteryText = $"{vehicle.BatteryLevel}%";
        BatteryBrush = DeviceTexts.Battery(vehicle.BatteryLevel);
        StatusText = DeviceTexts.Vehicle(vehicle.Status);
        (StatusBackground, StatusForeground) = DeviceTexts.Tone(vehicle.Status);

        var age = now - vehicle.LastUpdatedAt;
        var stale = vehicle.LastUpdatedAt != default && age.TotalMinutes > ServiceConstants.StaleThresholdMinutes;
        UpdatedText = vehicle.LastUpdatedAt == default ? "—" : Texts.Ago(vehicle.LastUpdatedAt, now);
        UpdatedBrush = stale ? Texts.Brush("#EA580C") : (Brush)(Application.Current.TryFindResource("Text2") ?? Texts.Brush("#64748B"));

        var online = vehicle.Connection == ConnectionStatus.Online;
        ConnectionGlyph = online ? "" : "";
        ConnectionBrush = Texts.Brush(online ? "#10B981" : "#EF4444");
        ConnectionText = online ? "Kết nối tốt" : "Mất kết nối";
    }
}

/// <summary>Ô cổng sạc trong lưới cổng sạc của một Hub.</summary>
public sealed class PortTile
{
    public string Id { get; }
    public string NumberText { get; }
    public string StatusText { get; }
    public string DetailText { get; }
    public Brush Background { get; }
    public Brush Foreground { get; }
    public string ToolTipText { get; }

    public PortTile(ChargingPoint point)
    {
        Id = point.Id;
        NumberText = $"#{point.Number}";
        StatusText = DeviceTexts.Port(point.Status);
        DetailText = string.IsNullOrWhiteSpace(point.AssignedVehicleCode)
            ? $"{point.PowerKw:0.#} kW"
            : point.AssignedVehicleCode!;
        (Background, Foreground) = DeviceTexts.Tone(point.Status);
        ToolTipText = $"{point.Id} · {StatusText} · {point.PowerKw:0.#} kW · {point.TemperatureC:0}°C"
                      + (string.IsNullOrWhiteSpace(point.AssignedVehicleCode) ? "" : $" · xe {point.AssignedVehicleCode}");
    }
}

/// <summary>Nhóm cổng sạc theo Hub.</summary>
public sealed class HubPortGroup
{
    public string HubId { get; }
    public string Name { get; }
    public string SummaryText { get; }
    public Brush SummaryBrush { get; }
    public List<PortTile> Ports { get; }

    public HubPortGroup(MobilityHub hub)
    {
        HubId = hub.Id;
        Name = string.IsNullOrWhiteSpace(hub.ShortName) ? hub.Id : $"{hub.Id} · {hub.ShortName}";
        Ports = hub.ChargingPoints.OrderBy(p => p.Number).Select(p => new PortTile(p)).ToList();
        var free = hub.ChargingPoints.Count(p => p.Status == DeviceStatus.Available);
        var broken = hub.ChargingPoints.Count(p => p.Status is DeviceStatus.Faulty or DeviceStatus.Offline or DeviceStatus.Locked or DeviceStatus.UnderRepair);
        var power = hub.ChargingPoints.Where(p => p.Status == DeviceStatus.Charging).Sum(p => p.PowerKw);
        SummaryText = $"{free} trống · {broken} lỗi/offline · {power:0.#}/{hub.PowerLimitKw:0.#} kW";
        SummaryBrush = Texts.Brush(broken > 0 ? "#EA580C" : "#64748B");
    }
}

/// <summary>Mục điều hướng ở thanh bên; số đếm (badge) cập nhật theo dữ liệu mà không làm mất mục đang chọn.</summary>
public sealed class NavItem : Infrastructure.ObservableObject
{
    private string _badge = "";

    public NavItem(string glyph, string label)
    {
        Glyph = glyph;
        Label = label;
    }

    public string Glyph { get; }
    public string Label { get; }

    public string Badge
    {
        get => _badge;
        set
        {
            if (!SetProperty(ref _badge, value)) return;
            OnPropertyChanged(nameof(BadgeVisibility));
        }
    }

    public Visibility BadgeVisibility => string.IsNullOrEmpty(Badge) ? Visibility.Collapsed : Visibility.Visible;
}

/// <summary>Bộ lọc trạng thái xe ở trang Phương tiện &amp; cổng sạc.</summary>
public sealed class DeviceFilterOption
{
    public DeviceFilterOption(string label, params DeviceStatus[] statuses)
    {
        Label = label;
        Statuses = statuses;
    }

    public string Label { get; }
    public IReadOnlyList<DeviceStatus> Statuses { get; }
    public bool Matches(DeviceStatus status) => Statuses.Count == 0 || Statuses.Contains(status);
}
