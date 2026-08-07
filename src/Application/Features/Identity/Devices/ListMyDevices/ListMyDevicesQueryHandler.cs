using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Devices.ListMyDevices;

internal sealed class ListMyDevicesQueryHandler
    : IQueryHandler<ListMyDevicesQuery, IReadOnlyList<DeviceResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyDevicesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<DeviceResponse>>> Handle(
        ListMyDevicesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<DeviceResponse>>.Failure(DeviceErrors.NotAuthenticated);
        }

        var devices = await _dbContext.UserDevices.AsNoTracking()
            .Where(device => device.UserId == userId)
            .OrderByDescending(device => device.LastSeenAt)
            .Select(device => new DeviceResponse(
                device.Id,
                (short)device.Platform,
                device.DeviceName,
                device.DeviceIdentifier,
                device.AppVersion,
                device.OsVersion,
                device.PushToken != null && device.PushToken != "",
                device.LastSeenAt,
                device.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<DeviceResponse>>.Success(devices);
    }
}
