namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// Sosyal girişten gelen profil fotoğrafının gerçek bir fotoğraf olup
/// olmadığına bakar.
/// </summary>
public interface IProfileImageScreener
{
    /// <summary>
    /// Google, fotoğrafı olmayan hesaplar için baş harfi taşıyan düz renkli
    /// bir görsel üretiyor ve bunu gerçek fotoğrafla aynı adreste sunuyor.
    /// O görsel kaydedilince kullanıcı uygulamada pembe/mor bir daireyle
    /// görünüyor — bizim kendi yedek avatarımız duracakken.
    /// </summary>
    /// <returns>Kaydedilecek adres; üretilmiş bir görselse <c>null</c>.</returns>
    Task<string?> KeepIfRealPhotoAsync(string? imageUrl, CancellationToken cancellationToken);
}
