using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Identity.Devices;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class DeviceErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Device.NotAuthenticated",
        ErrorMessagesResource.Device_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "Device.UserNotFound",
        ErrorMessagesResource.Device_UserNotFound);

    internal static Error NotFound => Error.NotFound(
        "Device.NotFound",
        ErrorMessagesResource.Device_NotFound);
}
