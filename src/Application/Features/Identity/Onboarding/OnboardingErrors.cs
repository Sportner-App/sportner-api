using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Onboarding;

internal static class OnboardingErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Onboarding.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Onboarding.UserNotFound",
        "The user was not found.");

    internal static readonly Error ProfileRequired = Error.Conflict(
        "Onboarding.ProfileRequired",
        "The profile must be created before onboarding can be completed.");

    internal static readonly Error SportRequired = Error.Conflict(
        "Onboarding.SportRequired",
        "At least one sport with a skill level must be selected before onboarding can be completed.");
}
