using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;

namespace Sportner.Application.Features.Reviews.ListReviewsForEvent;

public sealed record ListReviewsForEventQuery(Guid EventId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ReviewResponse>>;

internal sealed class ListReviewsForEventQueryHandler
    : IQueryHandler<ListReviewsForEventQuery, PagedResult<ReviewResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListReviewsForEventQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ReviewResponse>>> Handle(
        ListReviewsForEventQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationRequest(request.Page, request.PageSize);

        var query = ReviewQueries.Project(_dbContext)
            .Where(review => review.EventId == request.EventId);

        if (_currentUser.UserId is { } viewerId)
        {
            var blockedIds = BlockQueries.BlockedUserIds(_dbContext, viewerId);
            query = query.Where(review =>
                !blockedIds.Contains(review.ReviewerUserId)
                && !blockedIds.Contains(review.ReviewedUserId));
        }

        query = query.OrderByDescending(review => review.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ReviewResponse>>.Success(
            PagedResult<ReviewResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
