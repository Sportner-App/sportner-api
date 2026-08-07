using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Devices;

internal static class DeviceErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Device.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Device.UserNotFound",
        "The user was not found.");

    internal static readonly Error NotFound = Error.NotFound(
        "Device.NotFound",
        "The device was not found.");
}
