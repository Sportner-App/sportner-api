using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Reviews.UpdateReview;

public sealed record UpdateReviewCommand(Guid ReviewId, short Rating, string? Comment)
    : ICommand<ReviewResponse>;

public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId).NotEmpty();
        RuleFor(command => command.Rating).InclusiveBetween((short)1, (short)5);
        RuleFor(command => command.Comment).MaximumLength(1000);
    }
}

internal sealed class UpdateReviewCommandHandler : ICommandHandler<UpdateReviewCommand, ReviewResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateReviewCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReviewResponse>> Handle(
        UpdateReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.NotAuthenticated);
        }

        var review = await _dbContext.Reviews
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ReviewId, cancellationToken);

        if (review is null)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.NotFound);
        }

        if (review.ReviewerUserId != userId)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.NotReviewer);
        }

        var utcNow = _timeProvider.GetUtcNow();
        review.Update(request.Rating, request.Comment, utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await ReviewRatingSync.SyncReviewedUserAsync(
            _dbContext,
            review.ReviewedUserId,
            utcNow,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await ReviewQueries.Project(_dbContext)
            .FirstAsync(candidate => candidate.Id == review.Id, cancellationToken);

        return Result<ReviewResponse>.Success(response);
    }
}
