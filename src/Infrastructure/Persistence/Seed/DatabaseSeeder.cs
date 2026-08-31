using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Domain.Badges;
using Sportner.Domain.Locations;
using Sportner.Domain.Moderation;
using Sportner.Domain.Quests;
using Sportner.Domain.Sports;

namespace Sportner.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds reference data and keeps it aligned with <see cref="SeedData"/>. Existing rows are
/// updated in place (never re-created) so foreign keys from events, user sports and reports
/// remain valid.
/// </summary>
public sealed class DatabaseSeeder : IDatabaseSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var added = 0;

        added += await SeedCitiesAsync(utcNow, cancellationToken);
        added += await SeedSportsAsync(utcNow, cancellationToken);
        added += await SeedBadgesAsync(utcNow, cancellationToken);
        added += await SeedQuestsAsync(utcNow, cancellationToken);
        added += await SeedReportReasonsAsync(utcNow, cancellationToken);

        var changed = _dbContext.ChangeTracker.HasChanges();

        if (!changed)
        {
            _logger.LogInformation("Database seeding: reference data already up to date.");
            return;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Database seeding completed. {Count} new reference rows added, existing rows synchronized.",
            added);
    }

    private async Task<int> SeedCitiesAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var existingCities = await _dbContext.Cities.ToListAsync(cancellationToken);
        var byPlateCode = existingCities.ToDictionary(city => city.PlateCode);
        var added = 0;

        foreach (var seed in SeedData.Cities)
        {
            if (byPlateCode.TryGetValue(seed.PlateCode, out var current))
            {
                current.Rename(seed.Name, utcNow);
                continue;
            }

            _dbContext.Cities.Add(City.Create(seed.PlateCode, seed.Name, utcNow));
            added++;
        }

        return added;
    }

    private async Task<int> SeedSportsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var existingSports = await _dbContext.Sports.ToListAsync(cancellationToken);
        var bySlug = existingSports.ToDictionary(
            sport => sport.Slug,
            StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var seed in SeedData.Sports)
        {
            if (bySlug.TryGetValue(seed.Slug, out var current))
            {
                current.Rename(seed.Name, utcNow);
                current.ChangeDisplayOrder(seed.DisplayOrder, utcNow);
                continue;
            }

            // Earlier revision of the catalog: rename in place to keep the identifier stable.
            if (seed.LegacySlug is not null && bySlug.TryGetValue(seed.LegacySlug, out var legacy))
            {
                legacy.Rename(seed.Name, utcNow);
                legacy.ChangeSlug(seed.Slug, utcNow);
                legacy.ChangeDisplayOrder(seed.DisplayOrder, utcNow);
                continue;
            }

            _dbContext.Sports.Add(Sport.Create(seed.Name, seed.DisplayOrder, utcNow, seed.Slug));
            added++;
        }

        return added;
    }

    private async Task<int> SeedBadgesAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var existingBadges = await _dbContext.Badges.ToListAsync(cancellationToken);
        var byCode = existingBadges.ToDictionary(
            badge => badge.Code,
            StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var seed in SeedData.Badges)
        {
            if (byCode.TryGetValue(seed.Code, out var current))
            {
                current.Rename(seed.Name, utcNow);
                current.UpdateDescription(seed.Description, utcNow);
                continue;
            }

            _dbContext.Badges.Add(Badge.Create(
                seed.Code,
                seed.Name,
                seed.Description,
                seed.IconPath,
                seed.Category,
                seed.Rarity,
                seed.ExperiencePoints,
                seed.DisplayOrder,
                utcNow));
            added++;
        }

        return added;
    }

    private async Task<int> SeedQuestsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        // Quests depend on badge ids — ensure badge seed ran first in this pass.
        var badgesByCode = await _dbContext.Badges
            .ToDictionaryAsync(badge => badge.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existingQuests = await _dbContext.Quests.ToListAsync(cancellationToken);
        var byCode = existingQuests.ToDictionary(
            quest => quest.Code,
            StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var seed in SeedData.Quests)
        {
            if (!badgesByCode.TryGetValue(seed.RewardBadgeCode, out var rewardBadge))
            {
                _logger.LogWarning(
                    "Skipping quest seed {QuestCode}: reward badge {BadgeCode} not found.",
                    seed.Code,
                    seed.RewardBadgeCode);
                continue;
            }

            if (byCode.TryGetValue(seed.Code, out var current))
            {
                current.Rename(seed.Title, utcNow);
                current.UpdateDescription(seed.Description, utcNow);
                current.ChangeSortOrder(seed.SortOrder, utcNow);
                continue;
            }

            _dbContext.Quests.Add(Quest.Create(
                seed.Code,
                seed.Title,
                seed.Description,
                seed.MetricCode,
                seed.TargetValue,
                rewardBadge.Id,
                seed.SortOrder,
                utcNow));
            added++;
        }

        return added;
    }

    private async Task<int> SeedReportReasonsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existingReasons = await _dbContext.ReportReasons.ToListAsync(cancellationToken);
        var byCode = existingReasons.ToDictionary(
            reason => reason.Code,
            StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var seed in SeedData.ReportReasons)
        {
            if (byCode.TryGetValue(seed.Code, out var current))
            {
                current.Rename(seed.Name, utcNow);
                current.UpdateDescription(seed.Description, utcNow);
                continue;
            }

            _dbContext.ReportReasons.Add(ReportReason.Create(
                seed.Code,
                seed.Name,
                seed.Description,
                seed.DisplayOrder,
                utcNow));
            added++;
        }

        return added;
    }
}
