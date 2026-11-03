using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Leaderboards;

/// <summary>
/// Bir semtin bir aydaki lideri. Ay içinde lider değiştikçe güncellenir (yeni
/// lidere "1. sıraya çıktın" bildirimi için); ay kapanınca taç verilir ve
/// kayıt sabitlenir. Profildeki taçlar bu kayıtlardan okunur.
/// </summary>
public class DistrictLeader : AggregateRoot
{
    private DistrictLeader()
    {
    }

    public string City { get; private set; } = null!;

    public string District { get; private set; } = null!;

    /// <summary>Ayın ilk günü (İstanbul takvimi).</summary>
    public DateOnly Month { get; private set; }

    public Guid UserId { get; private set; }

    public int EventCount { get; private set; }

    public DateTimeOffset? CrownedAt { get; private set; }

    public static DistrictLeader Create(
        string city,
        string district,
        DateOnly month,
        Guid userId,
        int eventCount,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(district))
        {
            throw new DomainException("City and district are required.");
        }

        if (month.Day != 1)
        {
            throw new DomainException("Month must be the first day of the month.");
        }

        return new DistrictLeader
        {
            Id = Guid.NewGuid(),
            City = city,
            District = district,
            Month = month,
            UserId = userId,
            EventCount = eventCount,
            CreatedAt = utcNow
        };
    }

    /// <returns>Lider değiştiyse true.</returns>
    public bool UpdateLeader(Guid userId, int eventCount, DateTimeOffset utcNow)
    {
        if (CrownedAt is not null)
        {
            throw new DomainException("A crowned month cannot change.");
        }

        var changed = UserId != userId;
        UserId = userId;
        EventCount = eventCount;
        UpdatedAt = utcNow;
        return changed;
    }

    public void Crown(DateTimeOffset utcNow)
    {
        CrownedAt ??= utcNow;
        UpdatedAt = utcNow;
    }
}
