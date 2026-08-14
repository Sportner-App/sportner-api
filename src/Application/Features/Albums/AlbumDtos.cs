using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Albums;

internal static class AlbumErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Album.NotAuthenticated",
        "Authentication is required.");

    internal static readonly Error NotFound = Error.NotFound(
        "Album.NotFound",
        "The album was not found.");

    internal static readonly Error Forbidden = Error.Forbidden(
        "Album.Forbidden",
        "You are not allowed to access this album.");

    internal static readonly Error NotOwner = Error.Forbidden(
        "Album.NotOwner",
        "Only the album owner can perform this action.");

    internal static readonly Error EventNotFound = Error.NotFound(
        "Album.EventNotFound",
        "The event was not found.");

    internal static readonly Error NotOrganizer = Error.Forbidden(
        "Album.NotOrganizer",
        "Only the event organizer can create event albums.");

    internal static readonly Error CannotUpload = Error.Forbidden(
        "Album.CannotUpload",
        "You are not allowed to upload media to this album.");

    internal static readonly Error InvalidMedia = Error.Validation(
        "Album.InvalidMedia",
        "Only image media (jpeg, png, webp) is supported.");

    internal static readonly Error MediaNotFound = Error.NotFound(
        "Album.MediaNotFound",
        "The album media was not found.");

    internal static readonly Error ProfileAlbumLimit = Error.Validation(
        "Album.ProfileAlbumLimit",
        $"A profile may have at most {Domain.Social.Album.MaxAlbumsPerProfile} albums.");

    internal static readonly Error EventAlbumLimit = Error.Validation(
        "Album.EventAlbumLimit",
        $"An event may have at most {Domain.Social.Album.MaxAlbumsPerEvent} albums.");

    internal static readonly Error InvalidVisibility = Error.Validation(
        "Album.InvalidVisibility",
        "Album visibility is invalid for this album kind.");
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
