using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Gamification;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class BadgeErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Badge.NotAuthenticated",
        ErrorMessagesResource.Badge_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "Badge.UserNotFound",
        ErrorMessagesResource.Badge_UserNotFound);

    internal static Error ShowcaseTooMany => Error.Validation(
        "Badge.ShowcaseTooMany",
        string.Format(
            ErrorMessagesResource.Badge_ShowcaseTooMany,
            Domain.Badges.UserBadge.MaxShowcaseSlots));

    internal static Error ShowcaseDuplicate => Error.Validation(
        "Badge.ShowcaseDuplicate",
        ErrorMessagesResource.Badge_ShowcaseDuplicate);

    internal static Error ShowcaseNotOwned => Error.Validation(
        "Badge.ShowcaseNotOwned",
        ErrorMessagesResource.Badge_ShowcaseNotOwned);
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
