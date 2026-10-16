using System.Text;

namespace Sportner.Application.Common.Search;

/// <summary>
/// Arama karşılaştırmaları için metni sadeleştirir: Türkçe harfleri ASCII
/// karşılıklarına indirger ve küçük harfe çevirir.
///
/// İki ayrı tuzağı birden kapatır:
///
/// 1. Noktalı/noktasız i. <c>lower()</c> ve <c>ToLowerInvariant()</c> için
///    "I" ile "İ" ayrı harfler; telefon klavyesi ilk harfi büyüttüğü için
///    kullanıcı "Istanbul" yazıyor, kayıtlı ad "İstanbul" olduğundan arama
///    boş dönüyordu.
/// 2. Aksanlar. "Yüzme" ya da "Koşu" yazabilmek Türkçe klavye gerektiriyor;
///    "yuzme" yazan kullanıcı sporu bulamıyordu.
///
/// <see cref="Normalize"/> hem bellekte çalışır hem de EF Core tarafından
/// veritabanındaki <c>sportner_search_normalize</c> fonksiyonuna çevrilir
/// (bkz. ModelBuilderExtensions). İki taraf aynı sonucu üretmek zorunda:
/// buradaki eşleme değişirse migration'daki <c>translate()</c> çağrısı da
/// değişmeli.
/// </summary>
public static class SearchText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Kullanıcı arama kutusuna boşluk bırakıyor; SQL tarafındaki
        // btrim() ile aynı davranmak için burada da kırpılıyor.
        var trimmed = value.Trim();
        var builder = new StringBuilder(trimmed.Length);

        foreach (var character in trimmed)
        {
            builder.Append(character switch
            {
                'Ç' or 'ç' => 'c',
                'Ğ' or 'ğ' => 'g',
                'İ' or 'ı' => 'i',
                'Ö' or 'ö' => 'o',
                'Ş' or 'ş' => 's',
                'Ü' or 'ü' => 'u',
                'Â' or 'â' => 'a',
                'Î' or 'î' => 'i',
                'Û' or 'û' => 'u',
                _ => char.ToLowerInvariant(character)
            });
        }

        return builder.ToString();
    }
}
