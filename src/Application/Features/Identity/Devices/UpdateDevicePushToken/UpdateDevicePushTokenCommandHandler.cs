using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Devices.UpdateDevicePushToken;

internal sealed class UpdateDevicePushTokenCommandHandler
    : ICommandHandler<UpdateDevicePushTokenCommand, DeviceResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateDevicePushTokenCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DeviceResponse>> Handle(
        UpdateDevicePushTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<DeviceResponse>.Failure(DeviceErrors.NotAuthenticated);
        }

        var device = await _dbContext.UserDevices
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.DeviceId && candidate.UserId == userId,
                cancellationToken);

        if (device is null)
        {
            return Result<DeviceResponse>.Failure(DeviceErrors.NotFound);
        }

        device.UpdatePushToken(request.PushToken, _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<DeviceResponse>.Success(device.ToResponse());
    }
}
