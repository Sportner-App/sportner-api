using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Reviews.ListReviewablePeers;

public sealed record ListReviewablePeersQuery(Guid EventId)
    : IQuery<IReadOnlyList<ReviewablePeerResponse>>;

internal sealed class ListReviewablePeersQueryHandler
    : IQueryHandler<ListReviewablePeersQuery, IReadOnlyList<ReviewablePeerResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListReviewablePeersQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ReviewablePeerResponse>>> Handle(
        ListReviewablePeersQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<ReviewablePeerResponse>>.Failure(
                ReviewErrors.NotAuthenticated);
        }

        var @event = await _dbContext.Events.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.EventId, cancellationToken);

        if (@event is null)
        {
            return Result<IReadOnlyList<ReviewablePeerResponse>>.Failure(ReviewErrors.EventNotFound);
        }

        if (@event.Status is not EventStatus.Completed)
        {
            return Result<IReadOnlyList<ReviewablePeerResponse>>.Failure(
                ReviewErrors.EventNotCompleted);
        }

        var me = await _dbContext.EventParticipants.AsNoTracking()
            .FirstOrDefaultAsync(
                participant =>
                    participant.EventId == request.EventId && participant.UserId == userId,
                cancellationToken);

        if (me is null || me.Status is not ParticipantStatus.Attended || !me.CanReview)
        {
            return Result<IReadOnlyList<ReviewablePeerResponse>>.Failure(ReviewErrors.NotEligible);
        }

        var alreadyReviewedIds = _dbContext.Reviews.AsNoTracking()
            .Where(review =>
                review.EventId == request.EventId && review.ReviewerUserId == userId)
            .Select(review => review.ReviewedUserId);

        var peers = await (
                from participant in _dbContext.EventParticipants.AsNoTracking()
                join profile in _dbContext.Profiles.AsNoTracking()
                    on participant.UserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                where participant.EventId == request.EventId
                    && participant.UserId != userId
                    && participant.Status == ParticipantStatus.Attended
                    && !alreadyReviewedIds.Contains(participant.UserId)
                orderby profile != null ? profile.Username : participant.UserId.ToString()
                select new ReviewablePeerResponse(
                    participant.UserId,
                    profile != null ? profile.Username : null,
                    profile != null ? profile.FirstName : null,
                    profile != null ? profile.ProfileImageUrl : null))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ReviewablePeerResponse>>.Success(peers);
    }
}
