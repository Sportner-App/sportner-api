using System.Text.RegularExpressions;
using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Quests;

public class Quest : AggregateRoot
{
    private static readonly Regex CodePattern = new(
        @"^[A-Z0-9_]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Quest()
    {
    }

    public string Code { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string MetricCode { get; private set; } = null!;

    public int TargetValue { get; private set; }

    public Guid RewardBadgeId { get; private set; }

    public short SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static Quest Create(
        string code,
        string title,
        string description,
        string metricCode,
        int targetValue,
        Guid rewardBadgeId,
        short sortOrder,
        DateTimeOffset utcNow)
    {
        if (rewardBadgeId == Guid.Empty)
        {
            throw new DomainException("Reward badge id is required.");
        }

        if (targetValue <= 0)
        {
            throw new DomainException("Target value must be greater than zero.");
        }

        return new Quest
        {
            Id = Guid.NewGuid(),
            Code = NormalizeCode(code),
            Title = NormalizeTitle(title),
            Description = NormalizeDescription(description),
            MetricCode = NormalizeMetricCode(metricCode),
            TargetValue = targetValue,
            RewardBadgeId = rewardBadgeId,
            SortOrder = NormalizeSortOrder(sortOrder),
            IsActive = true,
            CreatedAt = utcNow
        };
    }

    public void Rename(string title, DateTimeOffset utcNow)
    {
        var normalized = NormalizeTitle(title);
        if (string.Equals(Title, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Title = normalized;
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

    public void ChangeSortOrder(short sortOrder, DateTimeOffset utcNow)
    {
        var normalized = NormalizeSortOrder(sortOrder);
        if (SortOrder == normalized)
        {
            return;
        }

        SortOrder = normalized;
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

    private void Touch(DateTimeOffset utcNow) => UpdatedAt = utcNow;

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Quest code is required.");
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 100 || !CodePattern.IsMatch(normalized))
        {
            throw new DomainException("Quest code is invalid.");
        }

        return normalized;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Quest title is required.");
        }

        var normalized = title.Trim();
        if (normalized.Length > 150)
        {
            throw new DomainException("Quest title cannot exceed 150 characters.");
        }

        return normalized;
    }

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Quest description is required.");
        }

        var normalized = description.Trim();
        if (normalized.Length > 1000)
        {
            throw new DomainException("Quest description cannot exceed 1000 characters.");
        }

        return normalized;
    }

    private static string NormalizeMetricCode(string metricCode)
    {
        if (string.IsNullOrWhiteSpace(metricCode))
        {
            throw new DomainException("Metric code is required.");
        }

        var normalized = metricCode.Trim().ToLowerInvariant();
        if (normalized.Length > 100)
        {
            throw new DomainException("Metric code cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static short NormalizeSortOrder(short sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.");
        }

        return sortOrder;
    }
}
