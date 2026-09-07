using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Explore;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class ExploreErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Explore.NotAuthenticated",
        ErrorMessagesResource.Explore_NotAuthenticated);
}
