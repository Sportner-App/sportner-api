using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Identity.Sessions;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class SessionErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Session.NotAuthenticated",
        ErrorMessagesResource.Session_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "Session.UserNotFound",
        ErrorMessagesResource.Session_UserNotFound);

    internal static Error NotFound => Error.NotFound(
        "Session.NotFound",
        ErrorMessagesResource.Session_NotFound);
}
