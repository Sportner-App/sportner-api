using Sportner.Application.Common.Localization;
using Sportner.Application.Common.Results;
using Sportner.Domain.Challenges;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Challenges;

internal static class ChallengeErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Challenge.NotAuthenticated",
        ErrorMessagesResource.Challenge_NotAuthenticated);

    internal static Error NotFound => Error.NotFound(
        "Challenge.NotFound",
        ErrorMessagesResource.Challenge_NotFound);

    internal static Error NotRunning => Error.Conflict(
        "Challenge.NotRunning",
        ErrorMessagesResource.Challenge_NotRunning);

    internal static Error AlreadyCompleted => Error.Conflict(
        "Challenge.AlreadyCompleted",
        ErrorMessagesResource.Challenge_AlreadyCompleted);
}

public sealed record ChallengeResponse(
    Guid Id,
    string Title,
    string Description,
    string Icon,
    ChallengeMetric Metric,
    string? SportSlug,
    int TargetValue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? SponsorName,
    string? SponsorLogoUrl,
    string? RewardText,
    int ParticipantCount,
    int CompletedCount,
    bool IsJoined,
    int Progress,
    bool IsCompleted,
    DateTimeOffset? CompletedAt);

internal static class ChallengeMapping
{
    internal static ChallengeResponse ToResponse(
        Challenge challenge,
        string? sportSlug,
        int participantCount,
        int completedCount,
        ChallengeParticipant? participation,
        int progress) =>
        new(
            challenge.Id,
            CatalogLocalization.Resolve(challenge.Title, challenge.TitleEn),
            CatalogLocalization.Resolve(challenge.Description, challenge.DescriptionEn),
            challenge.Icon,
            challenge.Metric,
            sportSlug,
            challenge.TargetValue,
            challenge.StartsAt,
            challenge.EndsAt,
            challenge.SponsorName,
            challenge.SponsorLogoUrl,
            challenge.RewardText is null
                ? null
                : CatalogLocalization.Resolve(challenge.RewardText, challenge.RewardTextEn),
            participantCount,
            completedCount,
            participation is not null,
            Math.Min(progress, challenge.TargetValue),
            // Madalya işi saatlik çalışıyor; hedefe ulaşan kullanıcı ekranda beklemesin.
            participation is not null
                && (participation.CompletedAt is not null || progress >= challenge.TargetValue),
            participation?.CompletedAt);
}
