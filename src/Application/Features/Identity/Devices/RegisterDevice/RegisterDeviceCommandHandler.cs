using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.Devices.RegisterDevice;

internal sealed class RegisterDeviceCommandHandler
    : ICommandHandler<RegisterDeviceCommand, DeviceResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RegisterDeviceCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DeviceResponse>> Handle(
        RegisterDeviceCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<DeviceResponse>.Failure(DeviceErrors.NotAuthenticated);
        }

        // The aggregate performs the upsert, so its devices must be loaded first.
        var user = await _dbContext.Users
            .Include(candidate => candidate.Devices)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<DeviceResponse>.Failure(DeviceErrors.UserNotFound);
        }

        var normalizedIdentifier = request.DeviceIdentifier.Trim();
        var existed = user.Devices.Any(device =>
            string.Equals(device.DeviceIdentifier, normalizedIdentifier, StringComparison.Ordinal));

        var device = user.RegisterDevice(
            (DevicePlatform)request.Platform,
            request.DeviceIdentifier,
            _timeProvider.GetUtcNow(),
            request.DeviceName,
            request.AppVersion,
            request.OsVersion,
            request.PushToken);

        // Client-generated Guids can be tracked as Modified by EF; force insert for new rows.
        if (!existed)
        {
            _dbContext.MarkAsAdded(device);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<DeviceResponse>.Success(device.ToResponse());
    }
}
