namespace Sportner.Application.Common.Models;

/// <summary>
/// Offset-based pagination envelope for list queries.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount) =>
        new(items, page, pageSize, totalCount);
}
