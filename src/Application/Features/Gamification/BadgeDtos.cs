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
    short DisplayOrder);

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
    DateTimeOffset EarnedAt);
