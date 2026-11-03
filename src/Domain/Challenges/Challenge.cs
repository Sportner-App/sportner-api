using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Challenges;

/// <summary>
/// Süreli hedef: "Kasımda 3 etkinliğe katıl". Kullanıcı katıl diyerek girer;
/// ilerleme katılım kayıtlarından hesaplanır, tamamlayan madalyayı kalıcı kazanır.
/// Sponsor alanları ileride markalı challenge'lar için.
/// </summary>
public class Challenge : AggregateRoot
{
    private Challenge()
    {
    }

    public string Code { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string? TitleEn { get; private set; }

    public string Description { get; private set; } = null!;

    public string? DescriptionEn { get; private set; }

    /// <summary>FontAwesome ikon adı.</summary>
    public string Icon { get; private set; } = null!;

    public ChallengeMetric Metric { get; private set; }

    /// <summary>Yalnızca <see cref="ChallengeMetric.SportEventsPlayed"/> için.</summary>
    public Guid? SportId { get; private set; }

    public int TargetValue { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public short SortOrder { get; private set; }

    public string? SponsorName { get; private set; }

    public string? SponsorLogoUrl { get; private set; }

    public string? RewardText { get; private set; }

    public string? RewardTextEn { get; private set; }

    public bool IsActive { get; private set; }

    public static Challenge Create(
        string code,
        string title,
        string description,
        string icon,
        ChallengeMetric metric,
        int targetValue,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        short sortOrder,
        DateTimeOffset utcNow,
        string? titleEn = null,
        string? descriptionEn = null,
        Guid? sportId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Challenge code is required.");
        }

        if (targetValue <= 0)
        {
            throw new DomainException("Target value must be greater than zero.");
        }

        if (endsAt <= startsAt)
        {
            throw new DomainException("Challenge must end after it starts.");
        }

        if (metric == ChallengeMetric.SportEventsPlayed && sportId is null)
        {
            throw new DomainException("Sport challenges need a sport.");
        }

        return new Challenge
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToLowerInvariant(),
            Title = title.Trim(),
            TitleEn = titleEn?.Trim(),
            Description = description.Trim(),
            DescriptionEn = descriptionEn?.Trim(),
            Icon = icon,
            Metric = metric,
            SportId = sportId,
            TargetValue = targetValue,
            StartsAt = startsAt,
            EndsAt = endsAt,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = utcNow
        };
    }

    public bool IsRunning(DateTimeOffset utcNow) => IsActive && StartsAt <= utcNow && utcNow < EndsAt;
}
