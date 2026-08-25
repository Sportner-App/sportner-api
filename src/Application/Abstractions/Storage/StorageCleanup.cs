using Sportner.Application.Abstractions.Storage;

namespace Sportner.Application.Abstractions.Storage;

/// <summary>
/// Best-effort object storage deletes after a successful DB commit.
/// Failures are swallowed so orphan cleanup can be retried by a later job.
/// </summary>
public static class StorageCleanup
{
    public static async Task TryDeleteAsync(
        IFileStorage fileStorage,
        string bucket,
        string? path,
        CancellationToken cancellationToken = default)
    {
        var objectPath = ToObjectPath(path, bucket);

        if (string.IsNullOrWhiteSpace(objectPath))
        {
            return;
        }

        try
        {
            await fileStorage.DeleteAsync(bucket, objectPath, cancellationToken);
        }
        catch
        {
            // Intentionally ignored — DB is source of truth after commit.
        }
    }

    public static async Task TryDeleteManyAsync(
        IFileStorage fileStorage,
        string bucket,
        IEnumerable<string?> paths,
        CancellationToken cancellationToken = default)
    {
        foreach (var path in paths)
        {
            await TryDeleteAsync(fileStorage, bucket, path, cancellationToken);
        }
    }

    /// <summary>
    /// Accepts either a raw object path or a public storage URL written by
    /// <see cref="IFileStorage.GetPublicUrl"/>.
    /// </summary>
    internal static string? ToObjectPath(string? stored, string bucket)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        var trimmed = stored.Trim();
        var marker = $"/object/public/{bucket}/";
        var index = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (index >= 0)
        {
            return Uri.UnescapeDataString(trimmed[(index + marker.Length)..]);
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return trimmed.TrimStart('/');
    }
}
