using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

public class PostMedia : AuditableEntity
{
    private PostMedia()
    {
    }

    public Guid PostId { get; private set; }

    public MediaType MediaType { get; private set; }

    public string StoragePath { get; private set; } = null!;

    public string FileName { get; private set; } = null!;

    public string MimeType { get; private set; } = null!;

    public long FileSize { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public int? DurationSeconds { get; private set; }

    public short DisplayOrder { get; private set; }

    internal static PostMedia Create(
        Guid postId,
        MediaType mediaType,
        string storagePath,
        string fileName,
        string mimeType,
        long fileSize,
        short displayOrder,
        DateTimeOffset utcNow,
        int? width = null,
        int? height = null,
        int? durationSeconds = null)
    {
        if (postId == Guid.Empty)
        {
            throw new DomainException("Post id is required.");
        }

        if (!Enum.IsDefined(mediaType))
        {
            throw new DomainException("Media type is invalid.");
        }

        if (fileSize <= 0)
        {
            throw new DomainException("File size must be greater than zero.");
        }

        EnsurePositiveOptional(width, "Width");
        EnsurePositiveOptional(height, "Height");
        EnsurePositiveOptional(durationSeconds, "Duration seconds");

        if (mediaType is MediaType.Image && durationSeconds is not null)
        {
            throw new DomainException("Image media cannot contain duration metadata.");
        }

        return new PostMedia
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            MediaType = mediaType,
            StoragePath = NormalizeRequiredText(storagePath, "Storage path"),
            FileName = NormalizeBoundedText(fileName, 255, "File name"),
            MimeType = NormalizeBoundedText(mimeType, 100, "MIME type"),
            FileSize = fileSize,
            Width = width,
            Height = height,
            DurationSeconds = durationSeconds,
            DisplayOrder = NormalizeDisplayOrder(displayOrder),
            CreatedAt = utcNow
        };
    }

    internal void ChangeDisplayOrder(short displayOrder, DateTimeOffset utcNow)
    {
        var normalized = NormalizeDisplayOrder(displayOrder);

        if (DisplayOrder == normalized)
        {
            return;
        }

        DisplayOrder = normalized;
        Touch(utcNow);
    }

    internal void UpdateDimensions(int? width, int? height, DateTimeOffset utcNow)
    {
        EnsurePositiveOptional(width, "Width");
        EnsurePositiveOptional(height, "Height");

        if (Width == width && Height == height)
        {
            return;
        }

        Width = width;
        Height = height;
        Touch(utcNow);
    }

    internal void UpdateVideoDuration(int? durationSeconds, DateTimeOffset utcNow)
    {
        if (MediaType is MediaType.Image)
        {
            throw new DomainException("Image media cannot receive video duration.");
        }

        EnsurePositiveOptional(durationSeconds, "Duration seconds");

        if (DurationSeconds == durationSeconds)
        {
            return;
        }

        DurationSeconds = durationSeconds;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static short NormalizeDisplayOrder(short displayOrder)
    {
        if (displayOrder <= 0)
        {
            throw new DomainException("Display order must be greater than zero.");
        }

        return displayOrder;
    }

    private static void EnsurePositiveOptional(int? value, string fieldName)
    {
        if (value is <= 0)
        {
            throw new DomainException($"{fieldName} must be greater than zero when provided.");
        }
    }

    private static string NormalizeRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string NormalizeBoundedText(string value, int maxLength, string fieldName)
    {
        var normalized = NormalizeRequiredText(value, fieldName);

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
