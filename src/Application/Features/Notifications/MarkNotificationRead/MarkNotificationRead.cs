using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Notifications.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;

internal sealed class MarkNotificationReadCommandHandler : ICommandHandler<MarkNotificationReadCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public MarkNotificationReadCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        MarkNotificationReadCommand request,
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

        notification.MarkAsRead(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
