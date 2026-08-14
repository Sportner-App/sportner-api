using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Reviews;

namespace Sportner.Application.Features.Reviews.CreateReview;

public sealed record CreateReviewCommand(
    Guid EventId,
    Guid ReviewedUserId,
    short Rating,
    string? Comment) : ICommand<ReviewResponse>;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.ReviewedUserId).NotEmpty();
        RuleFor(command => command.Rating).InclusiveBetween((short)1, (short)5);
        RuleFor(command => command.Comment).MaximumLength(1000);
    }
}

internal sealed class CreateReviewCommandHandler : ICommandHandler<CreateReviewCommand, ReviewResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;

    public CreateReviewCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _badgeAwarder = badgeAwarder;
        _questProgressTracker = questProgressTracker;
    }

    public async Task<Result<ReviewResponse>> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } reviewerUserId)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.NotAuthenticated);
        }

        if (reviewerUserId == request.ReviewedUserId)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.SelfReview);
        }

        var @event = await _dbContext.Events.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.EventId, cancellationToken);

        if (@event is null)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.EventNotFound);
        }

        if (@event.Status is not EventStatus.Completed)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.EventNotCompleted);
        }

        var participants = await _dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                participant.EventId == request.EventId
                && (participant.UserId == reviewerUserId
                    || participant.UserId == request.ReviewedUserId))
            .ToListAsync(cancellationToken);

        var reviewer = participants.FirstOrDefault(participant => participant.UserId == reviewerUserId);
        var reviewed = participants.FirstOrDefault(participant =>
            participant.UserId == request.ReviewedUserId);

        if (reviewer is null
            || reviewed is null
            || reviewer.Status is not ParticipantStatus.Attended
            || reviewed.Status is not ParticipantStatus.Attended
            || !reviewer.CanReview)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.NotEligible);
        }

        var alreadyExists = await _dbContext.Reviews.AsNoTracking()
            .AnyAsync(
                review =>
                    review.EventId == request.EventId
                    && review.ReviewerUserId == reviewerUserId
                    && review.ReviewedUserId == request.ReviewedUserId,
                cancellationToken);

        if (alreadyExists)
        {
            return Result<ReviewResponse>.Failure(ReviewErrors.AlreadyExists);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var priorReviewCount = await _dbContext.Reviews.AsNoTracking()
            .CountAsync(review => review.ReviewerUserId == reviewerUserId, cancellationToken);

        var review = Review.Create(
            request.EventId,
            reviewerUserId,
            request.ReviewedUserId,
            request.Rating,
            request.Comment,
            utcNow);

        _dbContext.Reviews.Add(review);

        var statistics = await _dbContext.UserStatistics
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == request.ReviewedUserId,
                cancellationToken);

        statistics?.IncreaseReviewCount(utcNow);

        if (priorReviewCount == 0)
        {
            await _badgeAwarder.TryAwardAsync(
                reviewerUserId,
                BadgeCodes.FirstReview,
                cancellationToken);
        }

        await _badgeAwarder.EvaluateAfterReviewCreatedAsync(reviewerUserId, cancellationToken);

        await _questProgressTracker.ReportAsync(
            reviewerUserId,
            QuestMetrics.ReviewsCreated,
            1,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recompute caches after the review row is visible to queries.
        await ReviewRatingSync.SyncReviewedUserAsync(
            _dbContext,
            request.ReviewedUserId,
            utcNow,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await ReviewQueries.Project(_dbContext)
            .FirstAsync(candidate => candidate.Id == review.Id, cancellationToken);

        return Result<ReviewResponse>.Success(response);
    }
}
