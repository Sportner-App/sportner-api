using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Reviews.GetReviewById;

public sealed record GetReviewByIdQuery(Guid ReviewId) : IQuery<ReviewResponse>;

internal sealed class GetReviewByIdQueryHandler : IQueryHandler<GetReviewByIdQuery, ReviewResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetReviewByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ReviewResponse>> Handle(
        GetReviewByIdQuery request,
        CancellationToken cancellationToken)
    {
        var review = await _dbContext.Reviews.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ReviewId, cancellationToken);

        if (review is null)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.NotFound);
        }

        // Reported reviews are hidden from everyone except the original reviewer.
        if (review.IsReported && _currentUser.UserId != review.ReviewerUserId)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.NotFound);
        }

        var response = await ReviewQueries.Project(_dbContext, includeReported: true)
            .FirstAsync(candidate => candidate.Id == request.ReviewId, cancellationToken);

        return Result<ReviewResponse>.Success(response);
    }
}
