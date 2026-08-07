using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports;

internal static class SportErrors
{
    internal static readonly Error NotFound = Error.NotFound(
        "Sport.NotFound",
        "The sport was not found.");

    internal static readonly Error NameTaken = Error.Conflict(
        "Sport.NameTaken",
        "A sport with this name already exists.");

    internal static readonly Error SlugTaken = Error.Conflict(
        "Sport.SlugTaken",
        "A sport with this slug already exists.");
}
