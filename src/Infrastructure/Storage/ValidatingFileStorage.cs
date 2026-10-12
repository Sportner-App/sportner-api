using Sportner.Application.Abstractions.Storage;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Infrastructure.Storage;

/// <summary>
/// Yuklenen icerigin gercekten iddia edilen turde oldugunu dosyanin ilk
/// baytlarindan dogrular.
///
/// Cagiranlar yalnizca istemcinin gonderdigi Content-Type basligini kontrol
/// ediyordu; o baslik tamamen istemci kontrolunde. "image/jpeg" etiketiyle
/// HTML yuklenip herkese acik depodan servis edilmesi saklanmis XSS'e kapi
/// aciyordu. Imza esleSmesi aranmasi bunu kapatir.
///
/// <see cref="IFileStorage"/> butun yukleme yollarinin tek gecis noktasi
/// oldugu icin sarmalayici burada duruyor — <see cref="ResizingFileStorage"/>
/// ile ayni gerekce. Sira onemli: dogrulama kucultmeden once calisir, cunku
/// kucultme cozemedigi icerigi oldugu gibi gecirir.
/// </summary>
internal sealed class ValidatingFileStorage : IFileStorage
{
    /// <summary>
    /// Imza tablosu. Her giris: (ofset, beklenen baytlar). Ayni tur icin birden
    /// fazla imza olabilir (ornegin farkli JPEG varyantlari).
    /// </summary>
    private static readonly Dictionary<string, byte[][]> Signatures =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [[0xFF, 0xD8, 0xFF]],
            ["image/jpg"] = [[0xFF, 0xD8, 0xFF]], // standart disi ama cagiranlar kabul ediyor
            ["image/png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
            ["image/gif"] = [
                "GIF87a"u8.ToArray(),
                "GIF89a"u8.ToArray()
            ],
            ["image/webp"] = [[0x52, 0x49, 0x46, 0x46]], // "RIFF"; WEBP etiketi 8. bayttan itibaren
            ["image/heic"] = [[0x66, 0x74, 0x79, 0x70]], // "ftyp"; 4. bayttan itibaren
            ["image/heif"] = [[0x66, 0x74, 0x79, 0x70]],
            ["video/mp4"] = [[0x66, 0x74, 0x79, 0x70]],
            ["video/quicktime"] = [[0x66, 0x74, 0x79, 0x70]],
            ["video/webm"] = [[0x1A, 0x45, 0xDF, 0xA3]], // EBML basligi
        };

    /// <summary>
    /// "ftyp" ve "RIFF" imzalari dosyanin basinda degil, sabit bir ofsette
    /// duruyor. Anahtar yoksa ofset sifirdir.
    /// </summary>
    private static readonly Dictionary<string, int> SignatureOffsets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/heic"] = 4,
            ["image/heif"] = 4,
            ["video/mp4"] = 4,
            ["video/quicktime"] = 4,
        };

    private const int HeaderLength = 16;

    private readonly IFileStorage _inner;

    public ValidatingFileStorage(IFileStorage inner)
    {
        _inner = inner;
    }

    public async Task<string> UploadAsync(
        string bucket,
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var validated = await EnsureMatchesContentTypeAsync(content, contentType, cancellationToken);

        return await _inner.UploadAsync(bucket, path, validated, contentType, cancellationToken);
    }

    public Task DeleteAsync(string bucket, string path, CancellationToken cancellationToken = default) =>
        _inner.DeleteAsync(bucket, path, cancellationToken);

    public string GetPublicUrl(string bucket, string path) =>
        _inner.GetPublicUrl(bucket, path);

    private static async Task<Stream> EnsureMatchesContentTypeAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!Signatures.TryGetValue(contentType, out var candidates))
        {
            // Tanimadigimiz bir tur: cagiranlarin izin verdigi liste zaten dar,
            // burada karar vermiyoruz.
            return content;
        }

        var header = new byte[HeaderLength];
        var read = await ReadHeaderAsync(content, header, cancellationToken);
        var offset = SignatureOffsets.GetValueOrDefault(contentType, 0);

        if (!candidates.Any(signature => Matches(header, read, offset, signature)))
        {
            throw new DomainException(
                $"Yuklenen dosyanin icerigi belirtilen turle ({contentType}) eslesmiyor.");
        }

        return Rewind(content, header, read);
    }

    private static async Task<int> ReadHeaderAsync(
        Stream content,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var read = 0;

        while (read < buffer.Length)
        {
            var chunk = await content.ReadAsync(
                buffer.AsMemory(read, buffer.Length - read),
                cancellationToken);

            if (chunk == 0)
            {
                break;
            }

            read += chunk;
        }

        return read;
    }

    private static bool Matches(byte[] header, int read, int offset, byte[] signature)
    {
        if (read < offset + signature.Length)
        {
            return false;
        }

        return header.AsSpan(offset, signature.Length).SequenceEqual(signature);
    }

    /// <summary>
    /// Basligi okumak akisi ilerletti. Aranabilen akislar geri sarilir; degilse
    /// okunan bayt bloguyla kalan icerik birlestirilir ki alt katman dosyayi
    /// bastan gorsun.
    /// </summary>
    private static Stream Rewind(Stream content, byte[] header, int read)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
            return content;
        }

        return new ConcatenatedStream(new MemoryStream(header, 0, read), content);
    }

    private sealed class ConcatenatedStream : Stream
    {
        private readonly Stream _first;
        private readonly Stream _second;
        private bool _firstDrained;

        public ConcatenatedStream(Stream first, Stream second)
        {
            _first = first;
            _second = second;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (!_firstDrained)
            {
                var read = await _first.ReadAsync(buffer, cancellationToken);

                if (read > 0)
                {
                    return read;
                }

                _firstDrained = true;
            }

            return await _second.ReadAsync(buffer, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _first.Dispose();
                _second.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
