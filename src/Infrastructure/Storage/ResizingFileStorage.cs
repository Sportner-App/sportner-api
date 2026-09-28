using Microsoft.Extensions.Logging;
using SkiaSharp;
using Sportner.Application.Abstractions.Storage;

namespace Sportner.Infrastructure.Storage;

/// <summary>
/// Yuklenen gorselleri depoya yazmadan once kucultur.
///
/// <see cref="IFileStorage"/> butun yukleme yollarinin tek gecis noktasi
/// oldugu icin sarmalayici burada duruyor: alti ayri cagri yerini tek tek
/// degistirmek yerine hepsi otomatik kapsaniyor, ileride eklenecekler de.
///
/// Iki sey bilincli olarak yapilmiyor:
/// - Format degistirilmiyor (PNG -> JPEG gibi). Cagiranlar contentType'i
///   veritabanina yolla birlikte yaziyor; formati degistirmek o kaydi
///   yalanci hale getirirdi.
/// - Gorsel olmayan icerik (video, PDF) hic acilmiyor, oldugu gibi gecer.
///
/// EXIF yeniden kodlama sirasinda dusuyor. Bu bir yan etki degil, kazanc:
/// telefon fotograflari cekim konumunu (GPS) tasiyor ve ham dosyayi herkese
/// acik depoya koymak kullanicinin ev adresini sizdirabilir. Yonlendirme
/// bilgisi silinmeden once piksellere uygulaniyor, yoksa fotograflar yan doner.
/// </summary>
internal sealed class ResizingFileStorage : IFileStorage
{
    private static readonly HashSet<string> ResizableContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
        };

    private readonly IFileStorage _inner;
    private readonly ILogger<ResizingFileStorage> _logger;

    public ResizingFileStorage(IFileStorage inner, ILogger<ResizingFileStorage> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        string bucket,
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var budget = ImageBudgets.For(bucket);

        if (budget is null || !ResizableContentTypes.Contains(contentType))
        {
            return await _inner.UploadAsync(bucket, path, content, contentType, cancellationToken);
        }

        Stream? resized = null;

        try
        {
            resized = await ResizeAsync(content, contentType, budget, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Bozuk ya da desteklenmeyen dosyada yukleme tamamen basarisiz
            // olmasin: orijinali gecir, sorunu logla.
            _logger.LogWarning(
                exception,
                "Image resize failed for {Bucket}/{Path}; storing the original.",
                bucket,
                path);
        }

        if (resized is null)
        {
            return await _inner.UploadAsync(bucket, path, content, contentType, cancellationToken);
        }

        await using (resized)
        {
            return await _inner.UploadAsync(bucket, path, resized, contentType, cancellationToken);
        }
    }

    public Task DeleteAsync(string bucket, string path, CancellationToken cancellationToken = default) =>
        _inner.DeleteAsync(bucket, path, cancellationToken);

    public string GetPublicUrl(string bucket, string path) =>
        _inner.GetPublicUrl(bucket, path);

    /// <returns>Kucultulmus akis, ya da dokunmaya deger bir sey yoksa null.</returns>
    private static async Task<Stream?> ResizeAsync(
        Stream source,
        string contentType,
        ImageBudget budget,
        CancellationToken cancellationToken)
    {
        // Skia senkron calisiyor ve akisin tamamini istiyor; istek akisi
        // seek edilemeyebildigi icin once bellege aliyoruz.
        using var buffer = new MemoryStream();
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        await source.CopyToAsync(buffer, cancellationToken);
        var originalLength = buffer.Length;
        buffer.Position = 0;

        using var codec = SKCodec.Create(buffer);

        if (codec is null)
        {
            return null;
        }

        using var original = SKBitmap.Decode(codec);

        if (original is null)
        {
            return null;
        }

        using var oriented = ApplyOrientation(original, codec.EncodedOrigin);

        var longestEdge = Math.Max(oriented.Width, oriented.Height);
        var scale = longestEdge > budget.MaxEdge ? (float)budget.MaxEdge / longestEdge : 1f;

        using var sized = scale < 1f
            ? oriented.Resize(
                new SKImageInfo(
                    Math.Max((int)Math.Round(oriented.Width * scale), 1),
                    Math.Max((int)Math.Round(oriented.Height * scale), 1)),
                new SKSamplingOptions(SKCubicResampler.Mitchell))
            : oriented.Copy();

        if (sized is null)
        {
            return null;
        }

        using var image = SKImage.FromBitmap(sized);
        // Yeni veri yalnizca piksellerden uretiliyor; EXIF/GPS burada dusuyor.
        using var encoded = image.Encode(FormatFor(contentType), budget.Quality);

        if (encoded is null || encoded.Size >= originalLength)
        {
            // Kucultme ise yaramadi (zaten optimize bir dosya): orijinali koru.
            return null;
        }

        var output = new MemoryStream(encoded.ToArray());
        output.Position = 0;
        return output;
    }

    /// <summary>
    /// EXIF yonlendirmesini piksellere isler. Metadata atildigi icin bu
    /// yapilmazsa telefondan gelen dikey fotograflar yan gorunur.
    /// </summary>
    private static SKBitmap ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.Default or SKEncodedOrigin.TopLeft)
        {
            return bitmap.Copy();
        }

        var swapsAxes = origin is SKEncodedOrigin.LeftTop
            or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom
            or SKEncodedOrigin.LeftBottom;

        var width = swapsAxes ? bitmap.Height : bitmap.Width;
        var height = swapsAxes ? bitmap.Width : bitmap.Height;

        var rotated = new SKBitmap(width, height);
        using var canvas = new SKCanvas(rotated);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Scale(-1, 1, width / 2f, 0);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.RotateDegrees(180, width / 2f, height / 2f);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Scale(1, -1, 0, height / 2f);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1, 0, bitmap.Height / 2f);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(0, height);
                canvas.RotateDegrees(270);
                canvas.Scale(1, -1, 0, bitmap.Height / 2f);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, height);
                canvas.RotateDegrees(270);
                break;
        }

        canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        canvas.Flush();

        return rotated;
    }

    private static SKEncodedImageFormat FormatFor(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/png" => SKEncodedImageFormat.Png,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg,
        };
}
