using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Quests.ListMyQuests;

public sealed record ListMyQuestsQuery : IQuery<IReadOnlyList<UserQuestItemResponse>>;

internal sealed class ListMyQuestsQueryHandler
    : IQueryHandler<ListMyQuestsQuery, IReadOnlyList<UserQuestItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyQuestsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserQuestItemResponse>>> Handle(
        ListMyQuestsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<UserQuestItemResponse>>.Failure(QuestErrors.NotAuthenticated);
        }

        var rows = await (
                from userQuest in _dbContext.UserQuests.AsNoTracking()
                join quest in _dbContext.Quests.AsNoTracking() on userQuest.QuestId equals quest.Id
                join badge in _dbContext.Badges.AsNoTracking()
                    on quest.RewardBadgeId equals badge.Id into badges
                from badge in badges.DefaultIfEmpty()
                where userQuest.UserId == userId && quest.IsActive
                orderby userQuest.Status, quest.SortOrder, quest.Code
                select new
                {
                    userQuest.Id,
                    QuestId = quest.Id,
                    quest.Code,
                    quest.Title,
                    quest.Description,
                    quest.MetricCode,
                    quest.TargetValue,
                    quest.RewardBadgeId,
                    RewardBadgeCode = badge != null ? badge.Code : null,
                    userQuest.Status,
                    userQuest.CurrentValue,
                    userQuest.CompletedAt,
                    userQuest.CreatedAt
                })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row =>
        {
            var percent = row.TargetValue <= 0
                ? 0
                : Math.Clamp((int)Math.Floor(row.CurrentValue * 100.0 / row.TargetValue), 0, 100);

            return new UserQuestItemResponse(
                row.Id,
                row.QuestId,
                row.Code,
                row.Title,
                row.Description,
                row.MetricCode,
                row.TargetValue,
                row.RewardBadgeId,
                row.RewardBadgeCode,
                (short)row.Status,
                row.CurrentValue,
                row.CompletedAt,
                percent,
                row.CreatedAt);
        }).ToList();

        return Result<IReadOnlyList<UserQuestItemResponse>>.Success(items);
    }
}
