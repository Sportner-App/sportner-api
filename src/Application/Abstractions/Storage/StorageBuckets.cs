namespace Sportner.Application.Abstractions.Storage;

/// <summary>
/// Bucket names used with <see cref="IFileStorage"/>. Buckets are part of the stored path
/// contract, so they are fixed rather than configurable.
/// </summary>
public static class StorageBuckets
{
    public const string Avatars = "avatars";

    public const string PostMedia = "post-media";

    /// <summary>
    /// Özel mesaj eki: Supabase'de <b>private</b> bucket (11 Kas 2026). <see cref="IFileStorage.GetPublicUrl"/>
    /// burada çalışmaz; uygulamada medya mesajı gösterilecekse API, sohbet üyeliğini kontrol edip
    /// Supabase'in süreli imzalı bağlantısını (POST /storage/v1/object/sign/...) dönmeli.
    /// </summary>
    public const string ChatMedia = "chat-media";

    public const string Albums = "albums";

    public const string SportCovers = "sport-covers";
}
