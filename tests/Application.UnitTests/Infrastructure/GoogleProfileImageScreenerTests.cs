using FluentAssertions;
using SkiaSharp;
using Sportner.Infrastructure.Authentication;

namespace Sportner.Application.UnitTests.Infrastructure;

public sealed class GoogleProfileImageScreenerTests
{
    /// <summary>Google'ın ürettiği avatar: düz zemin + tek renk harf.</summary>
    private static byte[] BasHarfGorseli()
    {
        using var bitmap = new SKBitmap(96, 96);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(236, 64, 122));
        using var kalem = new SKPaint { Color = SKColors.White };
        canvas.DrawRect(SKRect.Create(36, 28, 24, 40), kalem);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    /// <summary>Gerçek fotoğraf yerine: her pikseli farklı olan bir kare.</summary>
    private static byte[] FotografBenzeri()
    {
        using var bitmap = new SKBitmap(96, 96);
        var rastgele = new Random(42);
        for (var y = 0; y < 96; y++)
        {
            for (var x = 0; x < 96; x++)
            {
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)rastgele.Next(256),
                    (byte)rastgele.Next(256),
                    (byte)rastgele.Next(256)));
            }
        }
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    [Fact]
    public void IsGeneratedMonogram_SeesThroughGooglesLetterAvatar()
    {
        GoogleProfileImageScreener.IsGeneratedMonogram(BasHarfGorseli()).Should().BeTrue();
    }

    [Fact]
    public void IsGeneratedMonogram_LeavesARealPhotoAlone()
    {
        GoogleProfileImageScreener.IsGeneratedMonogram(FotografBenzeri()).Should().BeFalse();
    }
}
