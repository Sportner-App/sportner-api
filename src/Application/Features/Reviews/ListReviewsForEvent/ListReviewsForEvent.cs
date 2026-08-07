using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Reviews.ListReviewsForEvent;

public sealed record ListReviewsForEventQuery(Guid EventId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ReviewResponse>>;

internal sealed class ListReviewsForEventQueryHandler
    : IQueryHandler<ListReviewsForEventQuery, PagedResult<ReviewResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListReviewsForEventQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResult<ReviewResponse>>> Handle(
        ListReviewsForEventQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationRequest(request.Page, request.PageSize);

        var query = ReviewQueries.Project(_dbContext)
            .Where(review => review.EventId == request.EventId)
            .OrderByDescending(review => review.CreatedAt);

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
