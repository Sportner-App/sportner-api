namespace Sportner.Application.Common.Models;

/// <summary>
/// Cursor-based pagination envelope for feeds, notifications and messages.
/// The cursor is an opaque token the client echoes back to fetch the next page.
/// </summary>
public sealed record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor)
{
    public bool HasMore => NextCursor is not null;

    public static CursorPagedResult<T> Create(IReadOnlyList<T> items, string? nextCursor) =>
        new(items, nextCursor);
}
