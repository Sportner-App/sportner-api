using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Gamification;

internal static class BadgeErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Badge.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Badge.UserNotFound",
        "The user was not found.");

    internal static readonly Error ShowcaseTooMany = Error.Validation(
        "Badge.ShowcaseTooMany",
        $"At most {Domain.Badges.UserBadge.MaxShowcaseSlots} badges can be showcased.");

    internal static readonly Error ShowcaseDuplicate = Error.Validation(
        "Badge.ShowcaseDuplicate",
        "Showcase badge ids must be unique.");

    internal static readonly Error ShowcaseNotOwned = Error.Validation(
        "Badge.ShowcaseNotOwned",
        "Only earned badges can be showcased.");
}

public sealed record BadgeResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string IconPath,
    short Category,
    short Rarity,
    int ExperiencePoints,
    short DisplayOrder,
    bool? Earned = null);

public sealed record UserBadgeResponse(
    Guid Id,
    Guid BadgeId,
    string Code,
    string Name,
    string Description,
    string IconPath,
    short Category,
    short Rarity,
    int ExperiencePoints,
    DateTimeOffset EarnedAt,
    bool IsShowcased = false,
    short? ShowcaseOrder = null);

public sealed record BadgeProgressItemResponse(
    Guid BadgeId,
    string Code,
    string Name,
    string Description,
    string IconPath,
    short Category,
    short Rarity,
    bool Earned,
    int Current,
    int Target,
    int Percent);

public sealed record SetShowcasedBadgesRequest(IReadOnlyList<Guid> BadgeIds);
