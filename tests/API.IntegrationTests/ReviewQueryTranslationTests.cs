using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sportner.Application.Features.Reviews.ListReviewsForEvent;
using Sportner.Application.Features.Reviews.ListReviewsForUser;

namespace Sportner.API.IntegrationTests;

/// <summary>
/// ReviewQueries.Project combines a double LEFT JOIN with a projection into the
/// ReviewResponse record. Composing a further .Where(...)/.FirstAsync(predicate) on the
/// already-projected IQueryable used to fail Npgsql's SQL translation at runtime
/// ("could not be translated") even though it passes fine against the InMemory provider
/// used by the unit test suite - hence a real-database regression test here.
/// </summary>
public class ReviewQueryTranslationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReviewQueryTranslationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListReviewsForEvent_DoesNotThrowTranslationException()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var act = () => sender.Send(new ListReviewsForEventQuery(Guid.NewGuid()));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListReviewsForUser_DoesNotThrowTranslationException()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var act = () => sender.Send(new ListReviewsForUserQuery(Guid.NewGuid()));

        await act.Should().NotThrowAsync();
    }
}
