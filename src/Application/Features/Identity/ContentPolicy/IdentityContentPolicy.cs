using System.Globalization;
using System.Reflection;
using System.Text;

namespace Sportner.Application.Features.Identity.ContentPolicy;

/// <summary>
/// Kayıtta ve profil değişikliğinde kullanıcı adı, isim ve e-posta için içerik kuralları.
///
/// Metin karşılaştırmadan önce sadeleştirilir: Türkçe harfler, "p0rn0" gibi rakamlı yazımlar
/// ve "pooorno" gibi uzatmalar aynı biçime iner. Uzun ve masum bir kelimenin içinde geçmesi
/// zor olan kelimeler metnin her yerinde aranır; kısa olanlar ("am", "sik") ancak tek başına
/// bir parça olarak geçerse yakalanır, yoksa "Nazım" ya da "fizik" gibi adlar takılırdı.
/// Mevcut hesaplar etkilenmez; kurallar yalnızca yeni değerler yazılırken uygulanır.
/// </summary>
internal static class IdentityContentPolicy
{
    private static readonly string[] BlockedAnywhere =
    [
        "porn", "sikis", "siktir", "sikerim", "sikeyim", "orospu", "orosbu", "amcik", "amina",
        "yarak", "pezevenk", "kahpe", "gotveren", "fuck", "pusy", "onlyfans", "escort",
        "hitler", "nsfw", "nude", "hentai", "milf", "fahise",
    ];

    private static readonly HashSet<string> BlockedWords = new(StringComparer.Ordinal)
    {
        "sik", "sikik", "am", "got", "amk", "aq", "ibne", "gavat", "pic", "sex", "seks", "seksi",
        "dick", "cock", "cum", "anal", "slut", "whore", "bitch", "nazi", "isid", "isis",
    };

    private static readonly string[] ReservedAnywhere = ["sportner"];

    private static readonly HashSet<string> ReservedUsernames = new(StringComparer.Ordinal)
    {
        "admin", "administrator", "moderator", "mod", "yonetici", "destek", "support", "help",
        "yardim", "official", "resmi", "root", "system", "sistem", "staf", "team", "ekip",
    };

    private static readonly Lazy<HashSet<string>> DisposableDomains = new(LoadDisposableDomains);

    public static bool IsUsernameAllowed(string username) =>
        IsTextAllowed(username)
        && !ReservedUsernames.Contains(Collapse(username))
        && !ReservedAnywhere.Any(Collapse(username).Contains);

    public static bool IsNameAllowed(string? name) =>
        string.IsNullOrWhiteSpace(name) || IsTextAllowed(name);

    /// <summary>Alan adı ve üst alan adları listede mi bakılır ("x.mailinator.com" da yakalanır).</summary>
    public static bool IsEmailAllowed(string email)
    {
        var at = email.LastIndexOf('@');
        if (at < 0)
        {
            return true;
        }

        var domain = email[(at + 1)..].Trim().TrimEnd('.').ToLowerInvariant();
        while (domain.Contains('.'))
        {
            if (DisposableDomains.Value.Contains(domain))
            {
                return false;
            }

            domain = domain[(domain.IndexOf('.') + 1)..];
        }

        return true;
    }

    private static bool IsTextAllowed(string text)
    {
        // Tekrar sadeleştirmede teke indiği için "xxx" ayrıca ham metinde aranır.
        var collapsed = Collapse(text);
        if (text.Contains("xxx", StringComparison.OrdinalIgnoreCase)
            || BlockedAnywhere.Any(collapsed.Contains))
        {
            return false;
        }

        var parts = text.Split(
            [' ', '.', '_', '-', '\''],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return !parts.Select(Collapse).Any(BlockedWords.Contains);
    }

    /// <summary>
    /// Küçük harf, Türkçe harfler Latin karşılığına, rakamlar benzedikleri harfe; harf dışı
    /// her şey atılır ve art arda gelen aynı harfler teke indirilir ("Pooorn0" → "porno").
    /// Listeler de bu biçimde yazılı: "pussy" yerine "pusy".
    /// </summary>
    internal static string Collapse(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var raw in text.ToLower(CultureInfo.GetCultureInfo("tr-TR")))
        {
            var letter = raw switch
            {
                'ı' => 'i',
                'ş' => 's',
                'ğ' => 'g',
                'ü' => 'u',
                'ö' => 'o',
                'ç' => 'c',
                '0' => 'o',
                '1' => 'i',
                '3' => 'e',
                '4' => 'a',
                '5' => 's',
                '7' => 't',
                '@' => 'a',
                '$' => 's',
                _ => raw,
            };

            if (letter is < 'a' or > 'z')
            {
                continue;
            }

            if (builder.Length == 0 || builder[^1] != letter)
            {
                builder.Append(letter);
            }
        }

        return builder.ToString();
    }

    private static HashSet<string> LoadDisposableDomains()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("disposable-email-domains.txt", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);

        var domains = new HashSet<string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var domain = line.Trim().ToLowerInvariant();
            if (domain.Length > 0 && !domain.StartsWith('#'))
            {
                domains.Add(domain);
            }
        }

        return domains;
    }
}
