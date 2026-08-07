using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social.Friendships.AcceptFriendRequest;

public sealed record AcceptFriendRequestCommand(Guid FriendshipId) : ICommand<FriendshipResponse>;

internal sealed class AcceptFriendRequestCommandHandler
    : ICommandHandler<AcceptFriendRequestCommand, FriendshipResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IBadgeAwarder _badgeAwarder;

    public AcceptFriendRequestCommandHandler(
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

    public async Task<Result<FriendshipResponse>> Handle(
        AcceptFriendRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.NotAuthenticated);
        }

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(candidate => candidate.Id == request.FriendshipId, cancellationToken);

        if (friendship is null)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.NotFound);
        }

        if (friendship.AddresseeUserId != userId)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.NotAddressee);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var wasPending = friendship.Status == FriendshipStatus.Pending;

        friendship.Accept(utcNow);

        if (wasPending)
        {
            foreach (var participantId in new[] { friendship.RequesterUserId, friendship.AddresseeUserId })
            {
                var statistics = await _dbContext.UserStatistics
                    .FirstOrDefaultAsync(candidate => candidate.UserId == participantId, cancellationToken);

                statistics?.IncreaseFriendsCount(utcNow);

                await _badgeAwarder.TryAwardAsync(
                    participantId,
                    BadgeCodes.FirstFriend,
                    cancellationToken);
            }

            await _notificationPublisher.PublishAsync(
                friendship.RequesterUserId,
                NotificationType.FriendAccepted,
                "Arkadaşlık isteği kabul edildi",
                "Arkadaşlık isteğin kabul edildi.",
                NotificationEntityType.User,
                userId,
                userId,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<FriendshipResponse>.Success(
            await SocialQueries.ToFriendshipResponseAsync(_dbContext, friendship, cancellationToken));
    }
}
