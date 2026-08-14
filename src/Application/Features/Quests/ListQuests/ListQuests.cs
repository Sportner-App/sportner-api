using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Quests.ListQuests;

public sealed record ListQuestsQuery : IQuery<IReadOnlyList<QuestItemResponse>>;

internal sealed class ListQuestsQueryHandler
    : IQueryHandler<ListQuestsQuery, IReadOnlyList<QuestItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListQuestsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<QuestItemResponse>>> Handle(
        ListQuestsQuery request,
        CancellationToken cancellationToken)
    {
        var quests = await (
                from quest in _dbContext.Quests.AsNoTracking()
                where quest.IsActive
                join badge in _dbContext.Badges.AsNoTracking()
                    on quest.RewardBadgeId equals badge.Id into badges
                from badge in badges.DefaultIfEmpty()
                orderby quest.SortOrder, quest.Code
                select new
                {
                    quest.Id,
                    quest.Code,
                    quest.Title,
                    quest.Description,
                    quest.MetricCode,
                    quest.TargetValue,
                    quest.RewardBadgeId,
                    RewardBadgeCode = badge != null ? badge.Code : null,
                    quest.SortOrder
                })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, (short Status, int Current, DateTimeOffset? CompletedAt)>? progress = null;
        if (_currentUser.UserId is { } userId)
        {
            var rows = await _dbContext.UserQuests.AsNoTracking()
                .Where(userQuest => userQuest.UserId == userId)
                .Select(userQuest => new
                {
                    userQuest.QuestId,
                    userQuest.Status,
                    userQuest.CurrentValue,
                    userQuest.CompletedAt
                })
                .ToListAsync(cancellationToken);

            progress = rows.ToDictionary(
                row => row.QuestId,
                row => ((short)row.Status, row.CurrentValue, row.CompletedAt));
        }

        var items = quests.Select(quest =>
        {
            var current = 0;
            short? status = null;
            DateTimeOffset? completedAt = null;
            if (progress is not null && progress.TryGetValue(quest.Id, out var row))
            {
                status = row.Status;
                current = row.Current;
                completedAt = row.CompletedAt;
            }

            var percent = quest.TargetValue <= 0
                ? 0
                : Math.Clamp((int)Math.Floor(current * 100.0 / quest.TargetValue), 0, 100);

            return new QuestItemResponse(
                quest.Id,
                quest.Code,
                quest.Title,
                quest.Description,
                quest.MetricCode,
                quest.TargetValue,
                quest.RewardBadgeId,
                quest.RewardBadgeCode,
                quest.SortOrder,
                status,
                current,
                completedAt,
                percent);
        }).ToList();

        return Result<IReadOnlyList<QuestItemResponse>>.Success(items);
    }
}
