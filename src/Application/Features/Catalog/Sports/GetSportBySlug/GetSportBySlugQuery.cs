using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.GetSportBySlug;

public sealed record GetSportBySlugQuery(string Slug) : IQuery<SportResponse>;
