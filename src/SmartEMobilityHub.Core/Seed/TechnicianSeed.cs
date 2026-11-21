using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Seed;

/// <summary>Đội kỹ thuật (Bảng 46) — T-02 là người được đề xuất cho HUB-08 (TCOP03-03).</summary>
public static class TechnicianSeed
{
    public static void Apply(NetworkStore store)
    {
        store.Technicians.Add(new Technician
        {
            TechId = "T-01",
            Name = "Trần Quốc Bảo",
            Team = "Đội KT 01",
            Available = true,
            Location = new GeoPoint(10.8801, 106.8055)
        });

        // Cách HUB-08 khoảng 0,8 km — khớp Sub 2 §6.2.
        store.Technicians.Add(new Technician
        {
            TechId = "T-02",
            Name = "Nguyễn Minh Tuấn",
            Team = "Đội KT 02",
            Available = true,
            Location = new GeoPoint(10.8788, 106.8065)
        });

        // Đang bận — assignTechnician phải trả false (TCOP03-03).
        store.Technicians.Add(new Technician
        {
            TechId = "T-03",
            Name = "Lê Hoàng Nam",
            Team = "Đội KT 02",
            Available = false,
            Location = new GeoPoint(10.8735, 106.8032)
        });

        store.Technicians.Add(new Technician
        {
            TechId = "T-04",
            Name = "Phạm Đức Huy",
            Team = "Đội KT 01",
            Available = true,
            Location = new GeoPoint(10.8850, 106.8120)
        });

        store.Technicians.Add(new Technician
        {
            TechId = "T-05",
            Name = "Võ Thị Lan",
            Team = "Đội KT 03",
            Available = true,
            Location = new GeoPoint(10.8680, 106.7960)
        });

        store.Operators.Add(new Operator
        {
            OperatorId = "OP-01",
            Name = "Hoàng Vỹ",
            Role = "Quản trị vận hành",
            ShiftLabel = "Ca sáng · 07:00–15:00"
        });
    }
}
