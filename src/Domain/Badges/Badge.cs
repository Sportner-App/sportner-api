using System.Text.RegularExpressions;
using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Badges;

public class Badge : AggregateRoot
{
    private static readonly Regex CodePattern = new(
        @"^[A-Z0-9_]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Badge()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string IconPath { get; private set; } = null!;

    public BadgeCategory Category { get; private set; }

    public BadgeRarity Rarity { get; private set; }

    public int ExperiencePoints { get; private set; }

    public short DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static Badge Create(
        string code,
        string name,
        string description,
        string iconPath,
        BadgeCategory category,
        BadgeRarity rarity,
        int experiencePoints,
        short displayOrder,
        DateTimeOffset utcNow)
    {
        EnsureDefinedCategory(category);
        EnsureDefinedRarity(rarity);

        return new Badge
        {
            Id = Guid.NewGuid(),
            Code = NormalizeCode(code),
            Name = NormalizeName(name),
            Description = NormalizeDescription(description),
            IconPath = NormalizeIconPath(iconPath),
            Category = category,
            Rarity = rarity,
            ExperiencePoints = NormalizeExperiencePoints(experiencePoints),
            DisplayOrder = NormalizeDisplayOrder(displayOrder),
            IsActive = true,
            CreatedAt = utcNow
        };
    }

    public void Rename(string name, DateTimeOffset utcNow)
    {
        var normalized = NormalizeName(name);

        if (string.Equals(Name, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Name = normalized;
        Touch(utcNow);
    }

    public void UpdateDescription(string description, DateTimeOffset utcNow)
    {
        var normalized = NormalizeDescription(description);

        if (string.Equals(Description, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Description = normalized;
        Touch(utcNow);
    }

    public void ChangeIcon(string iconPath, DateTimeOffset utcNow)
    {
        var normalized = NormalizeIconPath(iconPath);

        if (string.Equals(IconPath, normalized, StringComparison.Ordinal))
        {
            return;
        }

        IconPath = normalized;
        Touch(utcNow);
    }

    public void ChangeCategory(BadgeCategory category, DateTimeOffset utcNow)
    {
        EnsureDefinedCategory(category);

        if (Category == category)
        {
            return;
        }

        Category = category;
        Touch(utcNow);
    }

    public void ChangeRarity(BadgeRarity rarity, DateTimeOffset utcNow)
    {
        EnsureDefinedRarity(rarity);

        if (Rarity == rarity)
        {
            return;
        }

        Rarity = rarity;
        Touch(utcNow);
    }

    public void ChangeExperiencePoints(int experiencePoints, DateTimeOffset utcNow)
    {
        var normalized = NormalizeExperiencePoints(experiencePoints);

        if (ExperiencePoints == normalized)
        {
            return;
        }

        ExperiencePoints = normalized;
        Touch(utcNow);
    }

    public void ChangeDisplayOrder(short displayOrder, DateTimeOffset utcNow)
    {
        var normalized = NormalizeDisplayOrder(displayOrder);

        if (DisplayOrder == normalized)
        {
            return;
        }

        DisplayOrder = normalized;
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

    public bool IsEarnable()
    {
        return IsActive;
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Badge code is required.");
        }

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length > 100)
        {
            throw new DomainException("Badge code cannot exceed 100 characters.");
        }

        if (!CodePattern.IsMatch(normalized))
        {
            throw new DomainException("Badge code may contain only A-Z, 0-9 and underscores.");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Badge name is required.");
        }

        var normalized = name.Trim();

        if (normalized.Length > 100)
        {
            throw new DomainException("Badge name cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Badge description is required.");
        }

        var normalized = description.Trim();

        if (normalized.Length > 1000)
        {
            throw new DomainException("Badge description cannot exceed 1000 characters.");
        }

        return normalized;
    }

    private static string NormalizeIconPath(string iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            throw new DomainException("Badge icon path is required.");
        }

        var normalized = iconPath.Trim();

        if (normalized.Length > 500)
        {
            throw new DomainException("Badge icon path cannot exceed 500 characters.");
        }

        return normalized;
    }

    private static int NormalizeExperiencePoints(int experiencePoints)
    {
        if (experiencePoints < 0)
        {
            throw new DomainException("Experience points cannot be negative.");
        }

        return experiencePoints;
    }

    private static short NormalizeDisplayOrder(short displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        return displayOrder;
    }

    private static void EnsureDefinedCategory(BadgeCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw new DomainException("Badge category is invalid.");
        }
    }

    private static void EnsureDefinedRarity(BadgeRarity rarity)
    {
        if (!Enum.IsDefined(rarity))
        {
            throw new DomainException("Badge rarity is invalid.");
        }
    }
}
