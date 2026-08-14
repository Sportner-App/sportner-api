namespace Sportner.Application.Common.Geo;

/// <summary>
/// Approximate bounding box for radius search (km). Day-1 without PostGIS;
/// tighten later with geography + GIST when extension is enabled.
/// </summary>
public static class GeoBoundingBox
{
    private const double EarthKmPerDegreeLat = 111.32;

    public static (decimal MinLat, decimal MaxLat, decimal MinLng, decimal MaxLng) For(
        decimal latitude,
        decimal longitude,
        double radiusKm)
    {
        var lat = (double)latitude;
        var lng = (double)longitude;
        var latDelta = radiusKm / EarthKmPerDegreeLat;
        var cosLat = Math.Cos(lat * Math.PI / 180.0);
        var lngDelta = cosLat is > 0.0001 or < -0.0001
            ? radiusKm / (EarthKmPerDegreeLat * Math.Abs(cosLat))
            : 180.0;

        return (
            (decimal)Math.Max(-90.0, lat - latDelta),
            (decimal)Math.Min(90.0, lat + latDelta),
            (decimal)Math.Max(-180.0, lng - lngDelta),
            (decimal)Math.Min(180.0, lng + lngDelta));
    }

    /// <summary>Great-circle distance in kilometers.</summary>
    public static double HaversineKm(
        decimal latitude1,
        decimal longitude1,
        decimal latitude2,
        decimal longitude2)
    {
        const double earthRadiusKm = 6371.0;
        var lat1 = DegreesToRadians((double)latitude1);
        var lat2 = DegreesToRadians((double)latitude2);
        var dLat = DegreesToRadians((double)(latitude2 - latitude1));
        var dLng = DegreesToRadians((double)(longitude2 - longitude1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2)
            * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
