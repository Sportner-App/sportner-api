using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Identity.SavedLocations;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class SavedLocationErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "SavedLocation.NotAuthenticated",
        ErrorMessagesResource.SavedLocation_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "SavedLocation.UserNotFound",
        ErrorMessagesResource.SavedLocation_UserNotFound);

    internal static Error NotFound => Error.NotFound(
        "SavedLocation.NotFound",
        ErrorMessagesResource.SavedLocation_NotFound);
}
