namespace Sportner.Application.Abstractions.Storage;

/// <summary>
/// Bucket names used with <see cref="IFileStorage"/>. Buckets are part of the stored path
/// contract, so they are fixed rather than configurable.
/// </summary>
public static class StorageBuckets
{
    public const string Avatars = "avatars";

    public const string IntroVideos = "intro-videos";

    public const string PostMedia = "post-media";

    public const string ChatMedia = "chat-media";

    public const string Albums = "albums";

    public const string SportCovers = "sport-covers";
}
