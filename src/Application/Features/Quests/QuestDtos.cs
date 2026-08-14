using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Quests;

internal static class QuestErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Quest.NotAuthenticated",
        "The request is not associated with an authenticated user.");
}

public sealed record QuestItemResponse(
    Guid Id,
    string Code,
    string Title,
    string Description,
    string MetricCode,
    int TargetValue,
    Guid RewardBadgeId,
    string? RewardBadgeCode,
    short SortOrder,
    short? Status,
    int CurrentValue,
    DateTimeOffset? CompletedAt,
    int Percent);

public sealed record UserQuestItemResponse(
    Guid Id,
    Guid QuestId,
    string Code,
    string Title,
    string Description,
    string MetricCode,
    int TargetValue,
    Guid RewardBadgeId,
    string? RewardBadgeCode,
    short Status,
    int CurrentValue,
    DateTimeOffset? CompletedAt,
    int Percent,
    DateTimeOffset CreatedAt);
