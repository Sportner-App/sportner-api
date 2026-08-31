using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social.Posts.LikePost;

public sealed record LikePostCommand(Guid PostId) : ICommand;

internal sealed class LikePostCommandHandler : ICommandHandler<LikePostCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public LikePostCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(LikePostCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(PostErrors.NotAuthenticated);
        }

        var post = await _dbContext.Posts
            .FirstOrDefaultAsync(candidate => candidate.Id == request.PostId, cancellationToken);

        if (post is null)
        {
            return Result.Failure(PostErrors.NotFound);
        }

        if (post.UserId == userId)
        {
            return Result.Failure(PostErrors.SelfLike);
        }

        if (await BlockQueries.BlockedPairExistsAsync(
                _dbContext,
                userId,
                post.UserId,
                cancellationToken))
        {
            return Result.Failure(PostErrors.Forbidden);
        }

        var alreadyLiked = await _dbContext.PostLikes.AsNoTracking()
            .AnyAsync(like => like.PostId == post.Id && like.UserId == userId, cancellationToken);

        if (alreadyLiked)
        {
            return Result.Failure(PostErrors.AlreadyLiked);
        }

        var utcNow = _timeProvider.GetUtcNow();
        _dbContext.PostLikes.Add(PostLike.Create(post.Id, userId, utcNow));
        post.IncrementLikeCount(utcNow);

        var likeCopy = await NotificationActor.TitleAsync(
            _dbContext,
            userId,
            "fotoğrafını beğendi",
            cancellationToken);

        await _notificationPublisher.PublishAsync(
            post.UserId,
            NotificationType.PostLiked,
            likeCopy,
            likeCopy,
            NotificationEntityType.Post,
            post.Id,
            userId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
