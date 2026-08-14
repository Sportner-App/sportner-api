using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Explore;

internal static class ExploreErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Explore.NotAuthenticated",
        "Authentication is required.");
}
