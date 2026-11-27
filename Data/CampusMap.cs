using CoreDomain = SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Data
{
    /// <summary>
    /// Bản đồ khu ĐHQG cho app Sinh viên: đổi toạ độ GPS của Hub (Core) sang km so với vị trí sinh viên,
    /// các điểm đến gợi ý và giới hạn thời gian sạc của từng Hub (DC-54).
    /// Vị trí sinh viên (gốc 0, 0) đặt gần lối ra ga Metro, giữa Hub Ga Metro và Hub Nhà văn hóa SV.
    /// </summary>
    public static class CampusMap
    {
        public const double OriginLat = 10.8790;
        public const double OriginLng = 106.8080;

        private const double KmPerDegreeLat = 110.574;
        private static readonly double KmPerDegreeLng = 111.320 * Math.Cos(OriginLat * Math.PI / 180);

        public static (double X, double Y) ToKm(double lat, double lng) =>
            ((lng - OriginLng) * KmPerDegreeLng, (lat - OriginLat) * KmPerDegreeLat);

        public static (double X, double Y) ToKm(CoreDomain.GeoPoint point) => ToKm(point.Lat, point.Lng);

        /// <summary>Điểm đến gợi ý ở Trang chủ (UC-01), toạ độ km so với vị trí sinh viên.</summary>
        public static readonly (string Name, double X, double Y)[] Destinations =
        {
            Destination("Thư viện Trung tâm", 10.8762, 106.8034),
            Destination("KTX Khu A", 10.8749, 106.7995),
            Destination("Ga Metro ĐHQG", 10.8801, 106.8094),
            Destination("Nhà văn hóa Sinh viên", 10.8789, 106.8085),
            Destination("Nhà học A1", 10.8772, 106.8063)
        };

        private static readonly Dictionary<string, int> MaxChargingMinutesByHub = new()
        {
            ["HUB-01"] = 180,
            ["HUB-04"] = 120,
            ["HUB-02"] = 240,
            ["HUB-11"] = 180
        };

        public const int DefaultMaxChargingMinutes = 180;

        /// <summary>Thời gian sạc tối đa mỗi lần tại Hub (UC-09).</summary>
        public static int MaxChargingMinutes(string hubId) =>
            MaxChargingMinutesByHub.TryGetValue(hubId, out var minutes) ? minutes : DefaultMaxChargingMinutes;

        /// <summary>Hub mặc định khi chưa chọn (Ga Metro).</summary>
        public const string DefaultHubId = "HUB-01";

        private static (string Name, double X, double Y) Destination(string name, double lat, double lng)
        {
            var (x, y) = ToKm(lat, lng);
            return (name, x, y);
        }
    }
}
