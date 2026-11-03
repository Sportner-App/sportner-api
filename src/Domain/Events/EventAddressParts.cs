using System.Text.RegularExpressions;

namespace Sportner.Domain.Events;

/// <summary>
/// Haritadan gelen adresin son kısmından semt ve şehri çıkarır:
/// "Göztepe, Tütüncü Mehmetefendi Cd. No:84, 34730 Kadıköy/İstanbul, Türkiye"
/// → ("Kadıköy", "İstanbul"). Kalıba uymayan adreste ikisi de null; o etkinlik
/// semt sıralamalarına girmez. Aynı ifade backfill migration'ında da kullanılıyor.
/// </summary>
public static partial class EventAddressParts
{
    public const string Pattern = @"(?:\d{5}\s+)?([^,/\d\s][^,/]*)/([^,/]+),\s*(?:Türkiye|Turkey)\s*$";

    [GeneratedRegex(Pattern, RegexOptions.CultureInvariant)]
    private static partial Regex DistrictRegex();

    public static (string? District, string? City) Parse(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return (null, null);
        }

        var match = DistrictRegex().Match(address);
        if (!match.Success)
        {
            return (null, null);
        }

        var district = match.Groups[1].Value.Trim();
        var city = match.Groups[2].Value.Trim();
        return district.Length == 0 || city.Length == 0 ? (null, null) : (district, city);
    }
}
