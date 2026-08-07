using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social.Friendships.SendFriendRequest;

public sealed record SendFriendRequestCommand(Guid AddresseeUserId) : ICommand<FriendshipResponse>;

internal sealed class SendFriendRequestCommandHandler
    : ICommandHandler<SendFriendRequestCommand, FriendshipResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public SendFriendRequestCommandHandler(
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

    public async Task<Result<FriendshipResponse>> Handle(
        SendFriendRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } requesterId)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.NotAuthenticated);
        }

        if (requesterId == request.AddresseeUserId)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.SelfRequest);
        }

        var addresseeExists = await _dbContext.Users.AsNoTracking()
            .AnyAsync(user => user.Id == request.AddresseeUserId, cancellationToken);

        if (!addresseeExists)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.UserNotFound);
        }

        var existing = await SocialQueries.FindBetweenAsync(
            _dbContext,
            requesterId,
            request.AddresseeUserId,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.Status is FriendshipStatus.Blocked)
            {
                return Result<FriendshipResponse>.Failure(FriendshipErrors.Blocked);
            }

            if (existing.Status is FriendshipStatus.Accepted or FriendshipStatus.Pending)
            {
                return Result<FriendshipResponse>.Failure(FriendshipErrors.AlreadyExists);
            }

            // Rejected rows keep the unique pair; remove and recreate a fresh pending request.
            _dbContext.Friendships.Remove(existing);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var friendship = Friendship.CreateRequest(requesterId, request.AddresseeUserId, utcNow);
        _dbContext.Friendships.Add(friendship);

        await _notificationPublisher.PublishAsync(
            request.AddresseeUserId,
            NotificationType.FriendRequest,
            "Yeni arkadaşlık isteği",
            "Sana bir arkadaşlık isteği gönderildi.",
            NotificationEntityType.User,
            requesterId,
            requesterId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<FriendshipResponse>.Success(
            await SocialQueries.ToFriendshipResponseAsync(_dbContext, friendship, cancellationToken));
    }
}
