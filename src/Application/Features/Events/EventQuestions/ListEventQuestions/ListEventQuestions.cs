using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Organizations;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.EventQuestions.ListEventQuestions;

public sealed record ListEventQuestionsQuery(Guid EventId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<EventQuestionResponse>>;

internal sealed class ListEventQuestionsQueryHandler
    : IQueryHandler<ListEventQuestionsQuery, PagedResult<EventQuestionResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListEventQuestionsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<EventQuestionResponse>>> Handle(
        ListEventQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var @event = await _dbContext.Events.AsNoTracking()
            .Where(candidate => candidate.Id == request.EventId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.OrganizerUserId,
                candidate.OrganizationId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (@event is null)
        {
            return Result<PagedResult<EventQuestionResponse>>.Failure(EventQuestionErrors.EventNotFound);
        }

        if (@event.OrganizationId is { } organizationId
            && !await OrganizationQueries.IsApprovedMemberAsync(
                _dbContext,
                organizationId,
                _currentUser.UserId,
                cancellationToken))
        {
            return Result<PagedResult<EventQuestionResponse>>.Failure(EventQuestionErrors.EventNotFound);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 50 ? 20 : request.PageSize;

        var rootsQuery = _dbContext.EventQuestions.AsNoTracking()
            .Where(question => question.EventId == @event.Id && question.ParentId == null);

        if (_currentUser.UserId is { } viewerId)
        {
            var blockedIds = BlockQueries.BlockedUserIds(_dbContext, viewerId);
            rootsQuery = rootsQuery.Where(question => !blockedIds.Contains(question.AuthorUserId));
        }

        var totalCount = await rootsQuery.CountAsync(cancellationToken);

        var roots = await rootsQuery
            .OrderByDescending(question => question.CreatedAt)
            .ThenByDescending(question => question.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rootIds = roots.Select(question => question.Id).ToList();
        var replies = rootIds.Count == 0
            ? []
            : await _dbContext.EventQuestions.AsNoTracking()
                .Where(question =>
                    question.EventId == @event.Id
                    && question.ParentId != null
                    && rootIds.Contains(question.ParentId.Value))
                .OrderBy(question => question.CreatedAt)
                .ThenBy(question => question.Id)
                .ToListAsync(cancellationToken);

        if (_currentUser.UserId is { } replyViewerId)
        {
            var blockedIds = await BlockQueries.BlockedUserIds(_dbContext, replyViewerId)
                .ToListAsync(cancellationToken);
            replies = replies
                .Where(reply => !blockedIds.Contains(reply.AuthorUserId))
                .ToList();
        }

        var participants = await EventQuestionAccess.ListParticipantUserIdsAsync(
            _dbContext,
            @event.Id,
            cancellationToken);

        var authorIds = roots.Select(item => item.AuthorUserId)
            .Concat(replies.Select(item => item.AuthorUserId))
            .Concat(replies.Where(item => item.ReplyToUserId is not null)
                .Select(item => item.ReplyToUserId!.Value))
            .Distinct()
            .ToList();

        var profiles = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => authorIds.Contains(profile.UserId))
            .Select(profile => new
            {
                profile.UserId,
                profile.Username,
                profile.FirstName,
                profile.ProfileImageUrl
            })
            .ToListAsync(cancellationToken);

        var profileById = profiles.ToDictionary(profile => profile.UserId);

        EventQuestionResponse Map(Domain.Events.EventQuestion question, IReadOnlyList<EventQuestionResponse> nested)
        {
            profileById.TryGetValue(question.AuthorUserId, out var profile);
            string? replyToUsername = null;
            if (question.ReplyToUserId is { } replyToUserId
                && profileById.TryGetValue(replyToUserId, out var replyProfile))
            {
                replyToUsername = replyProfile.Username;
            }

            return new EventQuestionResponse(
                question.Id,
                question.EventId,
                question.AuthorUserId,
                profile?.Username,
                profile?.FirstName,
                profile?.ProfileImageUrl,
                question.ParentId,
                question.ReplyToUserId,
                replyToUsername,
                question.Content,
                question.ReplyCount,
                (short)EventQuestionAccess.ResolveRole(
                    question.AuthorUserId,
                    @event.OrganizerUserId,
                    participants),
                question.CreatedAt,
                nested);
        }

        var repliesByRoot = replies
            .GroupBy(reply => reply.ParentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(reply => Map(reply, [])).ToList());

        var items = roots
            .Select(root => Map(
                root,
                repliesByRoot.GetValueOrDefault(root.Id) ?? []))
            .ToList();

        return Result<PagedResult<EventQuestionResponse>>.Success(
            PagedResult<EventQuestionResponse>.Create(items, page, pageSize, totalCount));
    }
}
