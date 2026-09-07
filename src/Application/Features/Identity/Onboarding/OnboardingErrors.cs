using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Identity.Onboarding;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class OnboardingErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Onboarding.NotAuthenticated",
        ErrorMessagesResource.Onboarding_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "Onboarding.UserNotFound",
        ErrorMessagesResource.Onboarding_UserNotFound);

    internal static Error ProfileRequired => Error.Conflict(
        "Onboarding.ProfileRequired",
        ErrorMessagesResource.Onboarding_ProfileRequired);

    internal static Error SportRequired => Error.Conflict(
        "Onboarding.SportRequired",
        ErrorMessagesResource.Onboarding_SportRequired);

    internal static Error PersonalDetailsRequired => Error.Conflict(
        "Onboarding.PersonalDetailsRequired",
        ErrorMessagesResource.Onboarding_PersonalDetailsRequired);

    internal static Error AvatarRequired => Error.Conflict(
        "Onboarding.AvatarRequired",
        ErrorMessagesResource.Onboarding_AvatarRequired);
}
