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
}
