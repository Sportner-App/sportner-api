using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Quests;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class QuestErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Quest.NotAuthenticated",
        ErrorMessagesResource.Quest_NotAuthenticated);
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
