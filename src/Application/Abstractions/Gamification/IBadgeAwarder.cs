using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.Abstractions.Gamification;

/// <summary>
/// Idempotent badge award helper used by producer modules.
/// Does not call <c>SaveChanges</c> — the caller owns the unit of work.
/// </summary>
public interface IBadgeAwarder
{
    /// <summary>
    /// Awards the badge when the definition is active and the user does not already have it.
    /// Also bumps <c>UserStatistics.BadgesCount</c> and publishes a <c>BadgeEarned</c> notification.
    /// </summary>
    Task TryAwardAsync(
        Guid userId,
        string badgeCode,
        CancellationToken cancellationToken = default);
}
