using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

public class AlbumMedia : AuditableEntity
{
    private AlbumMedia()
    {
    }

    public Guid AlbumId { get; private set; }

    public string StoragePath { get; private set; } = null!;

    public string FileName { get; private set; } = null!;

    public string MimeType { get; private set; } = null!;

    public long FileSize { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public short DisplayOrder { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    internal static AlbumMedia Create(
        Guid albumId,
        string storagePath,
        string fileName,
        string mimeType,
        long fileSize,
        short displayOrder,
        Guid uploadedByUserId,
        DateTimeOffset utcNow,
        int? width = null,
        int? height = null)
    {
        if (albumId == Guid.Empty)
        {
            throw new DomainException("Album id is required.");
        }

        if (uploadedByUserId == Guid.Empty)
        {
            throw new DomainException("Uploader user id is required.");
        }

        if (fileSize <= 0)
        {
            throw new DomainException("File size must be greater than zero.");
        }

        EnsurePositiveOptional(width, "Width");
        EnsurePositiveOptional(height, "Height");

        return new AlbumMedia
        {
            Id = Guid.NewGuid(),
            AlbumId = albumId,
            StoragePath = NormalizeRequired(storagePath, "Storage path", 500),
            FileName = NormalizeRequired(fileName, "File name", 255),
            MimeType = NormalizeRequired(mimeType, "MIME type", 100),
            FileSize = fileSize,
            Width = width,
            Height = height,
            DisplayOrder = NormalizeDisplayOrder(displayOrder),
            UploadedByUserId = uploadedByUserId,
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

    private static string NormalizeRequired(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
