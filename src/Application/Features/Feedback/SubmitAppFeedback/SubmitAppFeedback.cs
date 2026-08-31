using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Feedback;

namespace Sportner.Application.Features.Feedback.SubmitAppFeedback;

public sealed record SubmitAppFeedbackCommand(string Content)
    : ICommand<AppFeedbackResponse>;

public sealed class SubmitAppFeedbackCommandValidator
    : AbstractValidator<SubmitAppFeedbackCommand>
{
    public SubmitAppFeedbackCommandValidator()
    {
        RuleFor(command => command.Content)
            .NotEmpty()
            .MinimumLength(AppFeedback.MinContentLength)
            .MaximumLength(AppFeedback.MaxContentLength);
    }
}

internal sealed class SubmitAppFeedbackCommandHandler
    : ICommandHandler<SubmitAppFeedbackCommand, AppFeedbackResponse>
{
    private static readonly TimeSpan SubmitCooldown = TimeSpan.FromMinutes(2);

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SubmitAppFeedbackCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AppFeedbackResponse>> Handle(
        SubmitAppFeedbackCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<AppFeedbackResponse>.Failure(FeedbackErrors.NotAuthenticated);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var cooldownSince = utcNow - SubmitCooldown;

        var tooFrequent = await _dbContext.AppFeedbacks.AsNoTracking()
            .AnyAsync(
                feedback =>
                    feedback.UserId == userId
                    && feedback.CreatedAt >= cooldownSince,
                cancellationToken);

        if (tooFrequent)
        {
            return Result<AppFeedbackResponse>.Failure(FeedbackErrors.TooFrequent);
        }

        AppFeedback feedback;

        try
        {
            feedback = AppFeedback.Create(userId, request.Content, utcNow);
        }
        catch (DomainException)
        {
            return Result<AppFeedbackResponse>.Failure(FeedbackErrors.InvalidContent);
        }

        _dbContext.AppFeedbacks.Add(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AppFeedbackResponse>.Success(
            new AppFeedbackResponse(feedback.Id, feedback.CreatedAt));
    }
}
