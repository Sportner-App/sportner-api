namespace Sportner.Application.Common.Models;

/// <summary>
/// Shared offset pagination inputs. Handlers clamp these to safe bounds so no query is unbounded.
/// </summary>
public sealed record PaginationRequest(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}
