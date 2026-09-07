using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Identity.UserSports;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class UserSportErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "UserSport.NotAuthenticated",
        ErrorMessagesResource.UserSport_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "UserSport.UserNotFound",
        ErrorMessagesResource.UserSport_UserNotFound);

    internal static Error SportNotFound => Error.NotFound(
        "UserSport.SportNotFound",
        ErrorMessagesResource.UserSport_SportNotFound);

    internal static Error SportInactive => Error.Validation(
        "UserSport.SportInactive",
        ErrorMessagesResource.UserSport_SportInactive);

    internal static Error AlreadyAdded => Error.Conflict(
        "UserSport.AlreadyAdded",
        ErrorMessagesResource.UserSport_AlreadyAdded);

    internal static Error NotFound => Error.NotFound(
        "UserSport.NotFound",
        ErrorMessagesResource.UserSport_NotFound);
}
