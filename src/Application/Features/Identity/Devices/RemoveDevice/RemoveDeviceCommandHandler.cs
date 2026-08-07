using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Devices.RemoveDevice;

internal sealed class RemoveDeviceCommandHandler : ICommandHandler<RemoveDeviceCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RemoveDeviceCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(RemoveDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(DeviceErrors.NotAuthenticated);
        }

        // Removal cascades to the device's active sessions, so both collections must be loaded.
        var user = await _dbContext.Users
            .Include(candidate => candidate.Devices)
            .Include(candidate => candidate.Sessions)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(DeviceErrors.UserNotFound);
        }

        if (user.Devices.All(device => device.Id != request.DeviceId))
        {
            return Result.Failure(DeviceErrors.NotFound);
        }

        user.RemoveDevice(request.DeviceId, _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
