using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Features.Events;
using Sportner.Domain.Common.Enums;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Organizations;

public class ListOrganizationEventsQueryTests
{
    [Fact]
    public void OrganizationEventsQuery_TranslatesToSql_WhenOrderedBeforeProjection()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sportner_query_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new AppDbContext(options);
        var organizationId = Guid.NewGuid();
        var utcNow = DateTimeOffset.UtcNow;

        var events = dbContext.Events.AsNoTracking()
            .Where(@event =>
                @event.OrganizationId == organizationId
                && @event.Status != EventStatus.Cancelled
                && @event.EventDate >= utcNow.AddDays(-1))
            .OrderBy(@event => @event.EventDate);

        var query = EventQueries.ProjectListItems(dbContext, events);

        var act = () => query.ToQueryString();
        act.Should().NotThrow();
    }
}
