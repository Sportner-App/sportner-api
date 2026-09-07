using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Albums;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class AlbumErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Album.NotAuthenticated",
        ErrorMessagesResource.Album_NotAuthenticated);

    internal static Error NotFound => Error.NotFound(
        "Album.NotFound",
        ErrorMessagesResource.Album_NotFound);

    internal static Error Forbidden => Error.Forbidden(
        "Album.Forbidden",
        ErrorMessagesResource.Album_Forbidden);

    internal static Error NotOwner => Error.Forbidden(
        "Album.NotOwner",
        ErrorMessagesResource.Album_NotOwner);

    internal static Error EventNotFound => Error.NotFound(
        "Album.EventNotFound",
        ErrorMessagesResource.Album_EventNotFound);

    internal static Error NotOrganizer => Error.Forbidden(
        "Album.NotOrganizer",
        ErrorMessagesResource.Album_NotOrganizer);

    internal static Error CannotUpload => Error.Forbidden(
        "Album.CannotUpload",
        ErrorMessagesResource.Album_CannotUpload);

    internal static Error InvalidMedia => Error.Validation(
        "Album.InvalidMedia",
        ErrorMessagesResource.Album_InvalidMedia);

    internal static Error MediaNotFound => Error.NotFound(
        "Album.MediaNotFound",
        ErrorMessagesResource.Album_MediaNotFound);

    internal static Error ProfileAlbumLimit => Error.Validation(
        "Album.ProfileAlbumLimit",
        string.Format(
            ErrorMessagesResource.Album_ProfileAlbumLimit,
            Domain.Social.Album.MaxAlbumsPerProfile));

    internal static Error EventAlbumLimit => Error.Validation(
        "Album.EventAlbumLimit",
        string.Format(
            ErrorMessagesResource.Album_EventAlbumLimit,
            Domain.Social.Album.MaxAlbumsPerEvent));

    internal static Error InvalidVisibility => Error.Validation(
        "Album.InvalidVisibility",
        ErrorMessagesResource.Album_InvalidVisibility);
}

public sealed record AlbumMediaResponse(
    Guid Id,
    string StoragePath,
    string FileName,
    string MimeType,
    long FileSize,
    int? Width,
    int? Height,
    short DisplayOrder,
    Guid UploadedByUserId,
    DateTimeOffset CreatedAt);

public sealed record AlbumResponse(
    Guid Id,
    short Kind,
    Guid? OwnerUserId,
    Guid? EventId,
    string Title,
    string? Description,
    short Visibility,
    Guid? CoverMediaId,
    int MediaCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AlbumMediaResponse> Media);

public sealed record AlbumListItemResponse(
    Guid Id,
    short Kind,
    Guid? OwnerUserId,
    Guid? EventId,
    string Title,
    string? Description,
    short Visibility,
    Guid? CoverMediaId,
    int MediaCount,
    DateTimeOffset CreatedAt);
