using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Application.Features.Social.Posts;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Social.Comments.CreateComment;

public sealed record CreateCommentCommand(
    Guid PostId,
    string Content,
    IReadOnlyList<Guid>? MentionedUserIds = null) : ICommand<CommentResponse>;

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

        var mentions = await PostMentionResolver.ResolveAsync(
            _dbContext, userId, request.MentionedUserIds, cancellationToken);

        if (mentions.IsFailure)
        {
            return Result<CommentResponse>.Failure(mentions.Errors);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var comment = PostComment.CreateRoot(post.Id, userId, request.Content, utcNow);
        comment.SetMentions(mentions.Value!, utcNow);
        _dbContext.PostComments.Add(comment);
        post.IncrementCommentCount(utcNow);

        var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
            _dbContext, post.UserId, cancellationToken);

        await _notificationPublisher.PublishAsync(
            post.UserId,
            NotificationType.PostCommented,
            NotificationActor.Format(
                recipientLanguage,
                nameof(NotificationsResource.PostCommented_Title),
                await NotificationActor.PrefixAsync(_dbContext, userId, recipientLanguage, cancellationToken)),
            request.Content.Length <= 120 ? request.Content : request.Content[..117] + "...",
            NotificationEntityType.Post,
            post.Id,
            userId,
            cancellationToken);

        // Etiketlenenler yorum sahibinden ayri bildirim alir. Gonderi sahibi de
        // etiketlendiyse iki bildirim gelmesin diye o eleniyor.
        foreach (var mention in mentions.Value!.Where(m => m.UserId != post.UserId))
        {
            var language = await NotificationActor.ResolveRecipientLanguageAsync(
                _dbContext, mention.UserId, cancellationToken);

            await _notificationPublisher.PublishAsync(
                mention.UserId,
                NotificationType.PostMentioned,
                NotificationActor.Format(
                    language, nameof(NotificationsResource.PostMentioned_Title)),
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.PostMentioned_Body),
                    await NotificationActor.PrefixAsync(
                        _dbContext, userId, language, cancellationToken)),
                NotificationEntityType.Post,
                post.Id,
                userId,
                cancellationToken);
        }

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

        var mentions = comment.Mentions
            .Select(mention => new PostMentionResponse(mention.UserId, mention.Username))
            .ToList();

        // Yorum etiketler Include edilmeden yuklendiyse koleksiyon bos gelir.
        if (mentions.Count == 0)
        {
            mentions = await dbContext.CommentMentions.AsNoTracking()
                .Where(mention => mention.CommentId == comment.Id)
                .Select(mention => new PostMentionResponse(mention.UserId, mention.Username))
                .ToListAsync(cancellationToken);
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
            replyToUsername,
            mentions);
    }
}
