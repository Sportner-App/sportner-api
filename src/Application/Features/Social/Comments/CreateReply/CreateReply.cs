using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social.Comments.CreateComment;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social.Comments.CreateReply;

public sealed record CreateReplyCommand(Guid PostId, Guid ParentCommentId, string Content)
    : ICommand<CommentResponse>;

public sealed class CreateReplyCommandValidator : AbstractValidator<CreateReplyCommand>
{
    public CreateReplyCommandValidator()
    {
        RuleFor(command => command.PostId).NotEmpty();
        RuleFor(command => command.ParentCommentId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty().MaximumLength(1000);
    }
}

internal sealed class CreateReplyCommandHandler : ICommandHandler<CreateReplyCommand, CommentResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IBadgeAwarder _badgeAwarder;

    public CreateReplyCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher,
        IBadgeAwarder badgeAwarder)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
        _badgeAwarder = badgeAwarder;
    }

    public async Task<Result<CommentResponse>> Handle(
        CreateReplyCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<CommentResponse>.Failure(PostErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null || !user.CanCreateContent())
        {
            return Result<CommentResponse>.Failure(PostErrors.CannotCreateContent);
        }

        var post = await _dbContext.Posts
            .FirstOrDefaultAsync(candidate => candidate.Id == request.PostId, cancellationToken);

        if (post is null)
        {
            return Result<CommentResponse>.Failure(PostErrors.NotFound);
        }

        var parent = await _dbContext.PostComments
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.Id == request.ParentCommentId && candidate.PostId == request.PostId,
                cancellationToken);

        if (parent is null)
        {
            return Result<CommentResponse>.Failure(PostErrors.CommentNotFound);
        }

        // One nesting level only.
        if (parent.IsReply())
        {
            return Result<CommentResponse>.Failure(PostErrors.CommentNotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var reply = PostComment.CreateReply(
            post.Id,
            userId,
            parent.Id,
            request.Content,
            utcNow);

        _dbContext.PostComments.Add(reply);
        parent.IncrementReplyCount(utcNow);
        post.IncrementCommentCount(utcNow);

        await _notificationPublisher.PublishAsync(
            parent.UserId,
            NotificationType.CommentReplied,
            "Yorumuna yanıt verildi",
            request.Content.Length <= 120 ? request.Content : request.Content[..117] + "...",
            NotificationEntityType.Comment,
            reply.Id,
            userId,
            cancellationToken);

        await _badgeAwarder.EvaluateAfterCommentCreatedAsync(userId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CommentResponse>.Success(
            await CreateCommentCommandHandler.ToResponseAsync(_dbContext, reply, cancellationToken));
    }
}
