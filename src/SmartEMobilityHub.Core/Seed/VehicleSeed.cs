using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Seed;

/// <summary>
/// Xe dùng chung theo PLAN §4.1. Xe CHARGING được gán cổng còn trống ngay khi seed
/// để trạng thái cổng và xe không bao giờ mâu thuẫn.
/// </summary>
public static class VehicleSeed
{
    public static void Apply(NetworkStore store, DateTime now)
    {
        // Hero: sự cố SC-1011 / SC-1017 / SC-1022 / SC-1023.
        Add(store, "XE-062", VehicleType.EMotorbike, "Yadea Orla", "HUB-01", "A-03", 71, DeviceStatus.Faulty, now.AddMinutes(-4));
        Add(store, "XE-087", VehicleType.EBike, "Giant Ease", "HUB-08", "D-02", 55, DeviceStatus.UnderCheck, now.AddMinutes(-9));
        Add(store, "XE-118", VehicleType.EBike, "VinFast Evo200", "HUB-03", "B-01", 18, DeviceStatus.NeedsCharge, now.AddMinutes(-2));
        Add(store, "XE-204", VehicleType.EBike, "Pega eSmart", "HUB-08", "D-04", 42, DeviceStatus.Faulty, now.AddMinutes(-6));

        // HUB-01
        Add(store, "XE-101", VehicleType.EBike, "Giant Ease", "HUB-01", "A-01", 88, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-102", VehicleType.EBike, "VinFast Evo200", "HUB-01", "A-02", 76, DeviceStatus.Available, now.AddMinutes(-3));
        Add(store, "XE-103", VehicleType.EMotorbike, "Yadea Orla", "HUB-01", "A-04", 34, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-104", VehicleType.EBike, "Pega eSmart", "HUB-01", "A-05", 92, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-105", VehicleType.EMotorbike, "VinFast Vento", "HUB-01", "A-06", 65, DeviceStatus.Reserved, now.AddMinutes(-5));

        // HUB-02
        Add(store, "XE-106", VehicleType.EBike, "Giant Ease", "HUB-02", "C-01", 81, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-107", VehicleType.EMotorbike, "Yadea Orla", "HUB-02", "C-02", 22, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-108", VehicleType.EBike, "VinFast Evo200", "HUB-02", "C-03", 70, DeviceStatus.Available, now.AddMinutes(-4));

        // HUB-03
        Add(store, "XE-109", VehicleType.EBike, "Pega eSmart", "HUB-03", "B-02", 95, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-110", VehicleType.EMotorbike, "VinFast Vento", "HUB-03", "B-03", 44, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-111", VehicleType.EBike, "Giant Ease", "HUB-03", "B-04", 60, DeviceStatus.Available, now.AddMinutes(-3));

        // HUB-04
        Add(store, "XE-112", VehicleType.EBike, "VinFast Evo200", "HUB-04", "E-01", 73, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-113", VehicleType.EMotorbike, "Yadea Orla", "HUB-04", "E-02", 28, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-114", VehicleType.EBike, "Pega eSmart", "HUB-04", "E-03", 85, DeviceStatus.Available, now.AddMinutes(-6));

        // HUB-05
        Add(store, "XE-115", VehicleType.EBike, "Giant Ease", "HUB-05", "F-01", 90, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-116", VehicleType.EMotorbike, "VinFast Vento", "HUB-05", "F-02", 51, DeviceStatus.InUse, now.AddMinutes(-3));

        // HUB-06
        Add(store, "XE-117", VehicleType.EBike, "VinFast Evo200", "HUB-06", "G-01", 66, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-119", VehicleType.EMotorbike, "Yadea Orla", "HUB-06", "G-02", 31, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-120", VehicleType.EBike, "Pega eSmart", "HUB-06", "G-03", 78, DeviceStatus.Available, now.AddMinutes(-4));

        // HUB-07
        Add(store, "XE-121", VehicleType.EBike, "Giant Ease", "HUB-07", "H-01", 82, DeviceStatus.Reserved, now.AddMinutes(-5));
        Add(store, "XE-122", VehicleType.EBike, "VinFast Evo200", "HUB-07", "H-02", 57, DeviceStatus.Available, now.AddMinutes(-2));

        // HUB-08 — hero hub: 5 xe đang sạc tương ứng cổng 01, 02, 03, 05, 06.
        Add(store, "XE-123", VehicleType.EBike, "VinFast Evo200", "HUB-08", "D-05", 25, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-124", VehicleType.EBike, "Giant Ease", "HUB-08", "D-06", 19, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-125", VehicleType.EMotorbike, "Yadea Orla", "HUB-08", "D-07", 38, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-126", VehicleType.EMotorbike, "VinFast Vento", "HUB-08", "D-08", 47, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-127", VehicleType.EBike, "Pega eSmart", "HUB-08", "D-09", 12, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-128", VehicleType.EBike, "Giant Ease", "HUB-08", "D-10", 69, DeviceStatus.Relocating, now.AddMinutes(-2));
        Add(store, "XE-129", VehicleType.EBike, "VinFast Evo200", "HUB-08", "D-11", 74, DeviceStatus.Available, now.AddMinutes(-2));

        // 5 xe cho 5 reservation sắp tới tại HUB-08 (UC-OP-03 Exception 1a).
        Add(store, "XE-139", VehicleType.EBike, "Giant Ease", "HUB-08", "D-12", 61, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-140", VehicleType.EBike, "Pega eSmart", "HUB-08", "D-13", 83, DeviceStatus.Available, now.AddMinutes(-3));
        Add(store, "XE-141", VehicleType.EMotorbike, "Yadea Orla", "HUB-08", "D-14", 49, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-142", VehicleType.EBike, "VinFast Evo200", "HUB-08", "D-15", 72, DeviceStatus.Available, now.AddMinutes(-4));
        Add(store, "XE-143", VehicleType.EBike, "Giant Ease", "HUB-08", "D-16", 58, DeviceStatus.Available, now.AddMinutes(-2));

        // HUB-09
        Add(store, "XE-130", VehicleType.EMotorbike, "VinFast Vento", "HUB-09", "I-01", 29, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-131", VehicleType.EBike, "Giant Ease", "HUB-09", "I-02", 88, DeviceStatus.Available, now.AddMinutes(-2));
        Add(store, "XE-132", VehicleType.EBike, "Pega eSmart", "HUB-09", "I-03", 63, DeviceStatus.Available, now.AddMinutes(-3));

        // HUB-10
        Add(store, "XE-133", VehicleType.EMotorbike, "Yadea Orla", "HUB-10", "K-01", 41, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-134", VehicleType.EBike, "VinFast Evo200", "HUB-10", "K-02", 79, DeviceStatus.Available, now.AddMinutes(-2));

        // HUB-11
        Add(store, "XE-135", VehicleType.EBike, "Giant Ease", "HUB-11", "L-01", 33, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-136", VehicleType.EBike, "Pega eSmart", "HUB-11", "L-02", 87, DeviceStatus.Available, now.AddMinutes(-2));

        // HUB-12
        Add(store, "XE-137", VehicleType.EMotorbike, "Yadea Orla", "HUB-12", "M-01", 21, DeviceStatus.Charging, now.AddMinutes(-1));
        Add(store, "XE-138", VehicleType.EBike, "VinFast Evo200", "HUB-12", "M-02", 68, DeviceStatus.Available, now.AddMinutes(-2));
    }

    private static void Add(
        NetworkStore store,
        string code,
        VehicleType type,
        string model,
        string hubId,
        string slot,
        int battery,
        DeviceStatus status,
        DateTime updatedAt)
    {
        var hub = store.GetHub(hubId);
        var vehicle = new Vehicle
        {
            Code = code,
            Type = type,
            Model = model,
            HubId = hubId,
            Slot = slot,
            BatteryLevel = battery,
            Status = status,
            Connection = ConnectionStatus.Online,
            LastUpdatedAt = updatedAt
        };

        if (status == DeviceStatus.Charging)
        {
            var port = hub.ChargingPoints.FirstOrDefault(p => p.Status == DeviceStatus.Available);
            if (port is not null)
            {
                port.Status = DeviceStatus.Charging;
                port.AssignedVehicleCode = code;
                vehicle.AssignedPortId = port.Id;
            }
        }

        store.Vehicles.Add(vehicle);
    }
}
