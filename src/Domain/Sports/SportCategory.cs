using System.Text.RegularExpressions;
using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Sports;

/// <summary>
/// Reference data: the catalog grouping a <see cref="Sport"/> belongs to
/// (team, racket, combat…). Seeded on startup, never user-created.
/// </summary>
public sealed class SportCategory : AggregateRoot
{
    private static readonly Regex NonSlugCharacters = new(
        @"[^a-z0-9\-]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private SportCategory()
    {
    }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static SportCategory Create(
        string name,
        string slug,
        int displayOrder,
        DateTimeOffset utcNow,
        bool isActive = true)
    {
        return new SportCategory
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            Slug = NormalizeSlug(slug),
            DisplayOrder = NormalizeDisplayOrder(displayOrder),
            IsActive = isActive,
            CreatedAt = utcNow
        };
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

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Sport category name is required.");
        }

        var normalized = name.Trim();

        if (normalized.Length > 100)
        {
            throw new DomainException("Sport category name cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Sport category slug is required.");
        }

        var normalized = slug.Trim().ToLowerInvariant();

        if (normalized.Length > 100)
        {
            throw new DomainException("Sport category slug cannot exceed 100 characters.");
        }

        if (NonSlugCharacters.IsMatch(normalized)
            || normalized.StartsWith('-')
            || normalized.EndsWith('-')
            || normalized.Contains("--", StringComparison.Ordinal))
        {
            throw new DomainException("Sport category slug must be a URL-friendly identifier.");
        }

        return normalized;
    }

    private static int NormalizeDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        return displayOrder;
    }
}
