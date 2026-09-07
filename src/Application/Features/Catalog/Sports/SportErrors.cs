using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Catalog.Sports;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class SportErrors
{
    internal static Error NotFound => Error.NotFound(
        "Sport.NotFound",
        ErrorMessagesResource.Sport_NotFound);

    internal static Error NameTaken => Error.Conflict(
        "Sport.NameTaken",
        ErrorMessagesResource.Sport_NameTaken);

    internal static Error SlugTaken => Error.Conflict(
        "Sport.SlugTaken",
        ErrorMessagesResource.Sport_SlugTaken);

    internal static Error InvalidMedia => Error.Validation(
        "Sport.InvalidMedia",
        ErrorMessagesResource.Sport_InvalidMedia);
}
