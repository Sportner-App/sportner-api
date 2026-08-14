namespace Sportner.Application.Abstractions.Gamification;

/// <summary>
/// Idempotent badge award helper used by producer modules.
/// Does not call <c>SaveChanges</c> — the caller owns the unit of work
/// (except <see cref="SweepMarathonRunnersAsync"/> which saves its own batches).
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

    Task EvaluateAfterAttendanceAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task EvaluateAfterUserSportChangedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task EvaluateAfterCommentCreatedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task EvaluateAfterReportResolvedAsync(
        Guid reporterUserId,
        CancellationToken cancellationToken = default);

    Task EvaluateAfterFriendshipAcceptedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task EvaluateAfterReviewCreatedAsync(
        Guid reviewerUserId,
        CancellationToken cancellationToken = default);

    Task EvaluateAfterEventCompletedAsync(
        Guid organizerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Daily/periodic sweep for <c>MARATHON_RUNNER</c>. Owns SaveChanges per batch.
    /// </summary>
    Task SweepMarathonRunnersAsync(CancellationToken cancellationToken = default);
}
