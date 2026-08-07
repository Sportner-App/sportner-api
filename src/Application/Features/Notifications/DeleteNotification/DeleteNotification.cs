using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Notifications.DeleteNotification;

public sealed record DeleteNotificationCommand(Guid NotificationId) : ICommand;

internal sealed class DeleteNotificationCommandHandler : ICommandHandler<DeleteNotificationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteNotificationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        DeleteNotificationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(NotificationErrors.NotAuthenticated);
        }

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.Id == request.NotificationId
                    && candidate.RecipientUserId == userId,
                cancellationToken);

        if (notification is null)
        {
            return Result.Failure(NotificationErrors.NotFound);
        }

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
