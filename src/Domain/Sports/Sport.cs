using System.Text;
using System.Text.RegularExpressions;
using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Sports;

public class Sport : AggregateRoot
{
    private static readonly Regex NonSlugCharacters = new(
        @"[^a-z0-9\-]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Sport()
    {
    }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string? IconUrl { get; private set; }

    public string? CoverImageUrl { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static Sport Create(
        string name,
        int displayOrder,
        DateTimeOffset utcNow,
        string? slug = null,
        string? iconUrl = null,
        bool isActive = true)
    {
        var normalizedName = NormalizeName(name);
        var normalizedSlug = string.IsNullOrWhiteSpace(slug)
            ? GenerateSlug(normalizedName)
            : NormalizeSlug(slug);

        return new Sport
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Slug = normalizedSlug,
            IconUrl = NormalizeOptionalStoragePath(iconUrl),
            CoverImageUrl = null,
            DisplayOrder = NormalizeDisplayOrder(displayOrder),
            IsActive = isActive,
            CreatedAt = utcNow
        };
    }

    public void Activate(DateTimeOffset utcNow)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch(utcNow);
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch(utcNow);
    }

    public void Rename(string name, DateTimeOffset utcNow)
    {
        var normalizedName = NormalizeName(name);

        if (string.Equals(Name, normalizedName, StringComparison.Ordinal))
        {
            return;
        }

        Name = normalizedName;
        Touch(utcNow);
    }

    public void ChangeSlug(string slug, DateTimeOffset utcNow)
    {
        var normalizedSlug = NormalizeSlug(slug);

        if (string.Equals(Slug, normalizedSlug, StringComparison.Ordinal))
        {
            return;
        }

        Slug = normalizedSlug;
        Touch(utcNow);
    }

    public void ChangeIcon(string? iconUrl, DateTimeOffset utcNow)
    {
        IconUrl = NormalizeOptionalStoragePath(iconUrl);
        Touch(utcNow);
    }

    public void ChangeCoverImage(string? coverImageUrl, DateTimeOffset utcNow)
    {
        CoverImageUrl = NormalizeOptionalStoragePath(coverImageUrl);
        Touch(utcNow);
    }

    public void ChangeDisplayOrder(int displayOrder, DateTimeOffset utcNow)
    {
        var normalizedOrder = NormalizeDisplayOrder(displayOrder);

        if (DisplayOrder == normalizedOrder)
        {
            return;
        }

        DisplayOrder = normalizedOrder;
        Touch(utcNow);
    }

    public bool CanBeUsed()
    {
        return IsActive;
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Sport name is required.");
        }

        var normalized = name.Trim();

        if (normalized.Length > 100)
        {
            throw new DomainException("Sport name cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Sport slug is required.");
        }

        var normalized = slug.Trim().ToLowerInvariant();

        if (normalized.Length > 100)
        {
            throw new DomainException("Sport slug cannot exceed 100 characters.");
        }

        if (NonSlugCharacters.IsMatch(normalized)
            || normalized.StartsWith('-')
            || normalized.EndsWith('-')
            || normalized.Contains("--", StringComparison.Ordinal))
        {
            throw new DomainException("Sport slug must be a URL-friendly identifier.");
        }

        return normalized;
    }

    private static string GenerateSlug(string name)
    {
        var builder = new StringBuilder(name.Length);
        var previousWasHyphen = false;

        foreach (var character in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasHyphen = false;
                continue;
            }

            if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                if (builder.Length == 0 || previousWasHyphen)
                {
                    continue;
                }

                builder.Append('-');
                previousWasHyphen = true;
            }
        }

        var slug = builder.ToString().Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Unable to generate a valid sport slug from the name.");
        }

        if (slug.Length > 100)
        {
            slug = slug[..100].TrimEnd('-');
        }

        return NormalizeSlug(slug);
    }

    private static int NormalizeDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        return displayOrder;
    }

    private static string? NormalizeOptionalStoragePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path.Trim();
    }
}
