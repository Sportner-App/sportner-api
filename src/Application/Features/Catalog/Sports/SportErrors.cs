using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports;

internal static class SportErrors
{
    internal static readonly Error NotFound = Error.NotFound(
        "Sport.NotFound",
        "The sport was not found.");
}
