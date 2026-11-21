namespace SmartEMobilityHub.Core.Domain;

/// <summary>Toạ độ địa lý (Bảng 23: Hub.location, Bảng 46: Technician.locationLat/Lng).</summary>
public sealed class GeoPoint
{
    public double Lat { get; init; }
    public double Lng { get; init; }

    public GeoPoint() { }

    public GeoPoint(double lat, double lng)
    {
        Lat = lat;
        Lng = lng;
    }

    /// <summary>Khoảng cách đường chim bay (km) — dùng cho Technician.DistanceToHub (Bảng 47).</summary>
    public double DistanceKmTo(GeoPoint other)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(other.Lat - Lat);
        var dLng = ToRadians(other.Lng - Lng);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(Lat)) * Math.Cos(ToRadians(other.Lat))
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public GeoPoint Clone() => new(Lat, Lng);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
