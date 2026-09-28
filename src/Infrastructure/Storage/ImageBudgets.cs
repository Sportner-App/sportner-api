using Sportner.Application.Abstractions.Storage;

namespace Sportner.Infrastructure.Storage;

/// <param name="MaxEdge">Uzun kenarin ust siniri, piksel. Kucuk gorseller buyutulmez.</param>
/// <param name="Quality">JPEG/WebP kalitesi (1-100).</param>
internal sealed record ImageBudget(int MaxEdge, int Quality);

/// <summary>
/// Her kova, gorselin uygulamada en buyuk gorundugu yere gore siniri belirler.
/// Telefon kamerasi 4000px ve 4 MB uretiyor; avatar listede 40pt gosteriliyor,
/// yani ham dosyayi saklamak hem depolama hem indirme tarafinda saf israf.
/// </summary>
internal static class ImageBudgets
{
    private static readonly Dictionary<string, ImageBudget> ByBucket = new(StringComparer.Ordinal)
    {
        // En buyuk kullanim profil basligi (~96pt @3x).
        [StorageBuckets.Avatars] = new ImageBudget(512, 86),

        // Akista tam genislik gosteriliyor.
        [StorageBuckets.PostMedia] = new ImageBudget(1440, 84),

        // Sohbet balonu icinde, daha kucuk.
        [StorageBuckets.ChatMedia] = new ImageBudget(1280, 82),

        // Albumde tam ekran acilabiliyor.
        [StorageBuckets.Albums] = new ImageBudget(1920, 85),

        // Etkinlik kartlarinda kapak olarak tam genislik.
        [StorageBuckets.SportCovers] = new ImageBudget(1600, 85),
    };

    /// <summary>Tanimsiz kova = bilinmeyen kullanim; dokunmadan gecir.</summary>
    internal static ImageBudget? For(string bucket) =>
        ByBucket.TryGetValue(bucket, out var budget) ? budget : null;
}
