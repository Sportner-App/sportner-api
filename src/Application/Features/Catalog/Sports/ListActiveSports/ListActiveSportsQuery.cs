using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Common.Models;

namespace Sportner.Application.Features.Catalog.Sports.ListActiveSports;

public sealed record ListActiveSportsQuery(
    string? Search = null,
    string? CategorySlug = null,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedResult<SportResponse>>;
