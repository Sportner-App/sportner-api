using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social.Comments.CreateComment;

public sealed record CreateCommentCommand(Guid PostId, string Content) : ICommand<CommentResponse>;

public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(command => command.PostId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty().MaximumLength(1000);
    }
}

internal sealed class CreateCommentCommandHandler
    : ICommandHandler<CreateCommentCommand, CommentResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IBadgeAwarder _badgeAwarder;

    public CreateCommentCommandHandler(
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
        CreateCommentCommand request,
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

        if (post.UserId != userId
            && await BlockQueries.BlockedPairExistsAsync(
                _dbContext,
                userId,
                post.UserId,
                cancellationToken))
        {
            return Result<CommentResponse>.Failure(PostErrors.Forbidden);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var comment = PostComment.CreateRoot(post.Id, userId, request.Content, utcNow);
        _dbContext.PostComments.Add(comment);
        post.IncrementCommentCount(utcNow);

        await _notificationPublisher.PublishAsync(
            post.UserId,
            NotificationType.PostCommented,
            "Gönderine yorum yapıldı",
            request.Content.Length <= 120 ? request.Content : request.Content[..117] + "...",
            NotificationEntityType.Comment,
            comment.Id,
            userId,
            cancellationToken);

        await _badgeAwarder.EvaluateAfterCommentCreatedAsync(userId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CommentResponse>.Success(
            await ToResponseAsync(_dbContext, comment, cancellationToken));
    }

    internal static async Task<CommentResponse> ToResponseAsync(
        IApplicationDbContext dbContext,
        PostComment comment,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == comment.UserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        string? replyToUsername = null;
        if (comment.ReplyToUserId is { } replyToUserId)
        {
            replyToUsername = await dbContext.UserProfiles.AsNoTracking()
                .Where(candidate => candidate.UserId == replyToUserId)
                .Select(candidate => candidate.Username)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new CommentResponse(
            comment.Id,
            comment.PostId,
            comment.UserId,
            profile?.Username,
            profile?.FirstName,
            profile?.ProfileImageUrl,
            comment.ParentCommentId,
            comment.Content,
            comment.LikeCount,
            comment.ReplyCount,
            comment.CreatedAt,
            comment.ReplyToUserId,
            replyToUsername);
    }
}
