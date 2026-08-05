using System.Text.RegularExpressions;
using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Moderation;

public class ReportReason : AggregateRoot
{
    private static readonly Regex CodePattern = new(
        @"^[A-Z0-9_]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private ReportReason()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public short DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static ReportReason Create(
        string code,
        string name,
        string? description,
        short displayOrder,
        DateTimeOffset utcNow)
    {
        return new ReportReason
        {
            Id = Guid.NewGuid(),
            Code = NormalizeCode(code),
            Name = NormalizeName(name),
            Description = NormalizeDescription(description),
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

    public void UpdateDescription(string? description, DateTimeOffset utcNow)
    {
        var normalized = NormalizeDescription(description);

        if (string.Equals(Description, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Description = normalized;
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

    public bool IsSelectable()
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
            throw new DomainException("Report reason code is required.");
        }

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length > 100)
        {
            throw new DomainException("Report reason code cannot exceed 100 characters.");
        }

        if (!CodePattern.IsMatch(normalized))
        {
            throw new DomainException("Report reason code may contain only A-Z, 0-9 and underscores.");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Report reason name is required.");
        }

        var normalized = name.Trim();

        if (normalized.Length > 100)
        {
            throw new DomainException("Report reason name cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();

        if (normalized.Length > 1000)
        {
            throw new DomainException("Report reason description cannot exceed 1000 characters.");
        }

        return normalized;
    }

    private static short NormalizeDisplayOrder(short displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        return displayOrder;
    }
}
