using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Identity.UserProfiles;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class ProfileErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Profile.NotAuthenticated",
        ErrorMessagesResource.Profile_NotAuthenticated);

    internal static Error NotFound => Error.NotFound(
        "Profile.NotFound",
        ErrorMessagesResource.Profile_NotFound);

    internal static Error AlreadyExists => Error.Conflict(
        "Profile.AlreadyExists",
        ErrorMessagesResource.Profile_AlreadyExists);

    internal static Error UsernameTaken => Error.Conflict(
        "Profile.UsernameTaken",
        ErrorMessagesResource.Profile_UsernameTaken);

    internal static Error UsernameChangeTooSoon => Error.Conflict(
        "Profile.UsernameChangeTooSoon",
        ErrorMessagesResource.Profile_UsernameChangeTooSoon);

    internal static Error NotPublic => Error.Forbidden(
        "Profile.NotPublic",
        ErrorMessagesResource.Profile_NotPublic);

    internal static Error InvalidMedia => Error.Validation(
        "Profile.InvalidMedia",
        ErrorMessagesResource.Profile_InvalidMedia);

    internal static Error InvalidCity => Error.Validation(
        "Profile.InvalidCity",
        ErrorMessagesResource.Profile_InvalidCity);
}
