using Microsoft.Extensions.Logging;
using SkiaSharp;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Infrastructure.Authentication;

/// <inheritdoc />
public sealed class GoogleProfileImageScreener : IProfileImageScreener
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleProfileImageScreener> _logger;

    public GoogleProfileImageScreener(
        HttpClient httpClient,
        ILogger<GoogleProfileImageScreener> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> KeepIfRealPhotoAsync(
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        // Yalnızca Google'ın sunduğu görseller inceleniyor; başka bir adres
        // geldiğinde dokunmuyoruz.
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            || !uri.Host.EndsWith("googleusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            return imageUrl;
        }

        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(uri, cancellationToken);
            return IsGeneratedMonogram(bytes) ? null : imageUrl;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Ağ ya da çözümleme hatasında fotoğrafı koruyoruz: kayıt akışını
            // bir avatar kontrolü yüzünden bozmak en kötü sonuç olurdu.
            _logger.LogWarning(
                exception,
                "Profile image could not be screened; keeping the provider URL.");
            return imageUrl;
        }
    }

    /// <summary>
    /// Üretilmiş baş harf görselleri düz bir zemin ve tek renk harften oluşur:
    /// birkaç renk neredeyse tüm kareyi kaplar. Gerçek fotoğrafta aynı ölçüm
    /// onlarca farklı renk verir.
    /// </summary>
    public static bool IsGeneratedMonogram(byte[] bytes)
    {
        using var bitmap = SKBitmap.Decode(bytes);

        if (bitmap is null || bitmap.Width == 0 || bitmap.Height == 0)
        {
            return false;
        }

        const int Adim = 16;
        var sayac = new Dictionary<int, int>();
        var toplam = 0;

        for (var y = 0; y < Adim; y++)
        {
            for (var x = 0; x < Adim; x++)
            {
                var piksel = bitmap.GetPixel(
                    x * bitmap.Width / Adim,
                    y * bitmap.Height / Adim);

                // Renkler kabaca gruplanıyor: JPEG sıkıştırması aynı düz zemini
                // birbirine çok yakın birkaç tona bölebiliyor.
                var anahtar = (piksel.Red / 24 << 16) | (piksel.Green / 24 << 8) | (piksel.Blue / 24);
                sayac[anahtar] = sayac.GetValueOrDefault(anahtar) + 1;
                toplam++;
            }
        }

        var enYayginIki = sayac.Values.OrderByDescending(adet => adet).Take(2).Sum();
        return enYayginIki >= toplam * 0.95;
    }
}
