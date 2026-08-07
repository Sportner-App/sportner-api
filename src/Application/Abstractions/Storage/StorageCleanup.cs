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
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await fileStorage.DeleteAsync(bucket, path, cancellationToken);
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
}
