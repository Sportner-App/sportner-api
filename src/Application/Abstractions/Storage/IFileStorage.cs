namespace Sportner.Application.Abstractions.Storage;

/// <summary>
/// Object storage abstraction backed by Supabase Storage. The database stores only paths;
/// binaries never live in PostgreSQL.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Uploads (or overwrites) an object and returns the stored object path.
    /// </summary>
    Task<string> UploadAsync(
        string bucket,
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string bucket, string path, CancellationToken cancellationToken = default);

    string GetPublicUrl(string bucket, string path);
}
