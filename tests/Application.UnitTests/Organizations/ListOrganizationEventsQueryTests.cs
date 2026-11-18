using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Organizations.ListOrganizationEvents;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Organizations;
using Sportner.Domain.Sports;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

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
        var isMember = false;

        var events = dbContext.Events.AsNoTracking()
            .Where(@event =>
                @event.OrganizationId == organizationId
                && (isMember || !@event.IsOrganizationOnly)
                && @event.Status != EventStatus.Cancelled
                && @event.EventDate >= utcNow.AddDays(-1))
            .OrderBy(@event => @event.EventDate);

        var query = EventQueries.ProjectListItems(dbContext, events);

        var act = () => query.ToQueryString();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Handle_PublicClub_NonMemberSeesOnlyOpenEvents()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var viewer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var founder = TestUsers.CreateActive("+905552222222", time.GetUtcNow());

        var club = Organization.Create(
            founder.Id, "Açık Kulüp", null, null, "ABCD2345", true, time.GetUtcNow());
        var membership = OrganizationMember.CreateFounder(club.Id, founder.Id, time.GetUtcNow());
        var openEvent = CreateEvent(founder.Id, sport.Id, "Açık etkinlik", club.Id, false, time);
        var membersOnlyEvent = CreateEvent(founder.Id, sport.Id, "Üyelere özel", club.Id, true, time);

        db.Users.AddRange(viewer, founder);
        db.Sports.Add(sport);
        db.Organizations.Add(club);
        db.OrganizationMembers.Add(membership);
        db.Events.AddRange(openEvent, membersOnlyEvent);
        await db.SaveChangesAsync();

        var handler = new ListOrganizationEventsQueryHandler(db, new TestCurrentUser(viewer.Id), time);
        var result = await handler.Handle(new ListOrganizationEventsQuery(club.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle(item => item.Id == openEvent.Id);
    }

    [Fact]
    public async Task Handle_PrivateClub_NonMemberSeesOnlyOpenEvents()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var viewer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var founder = TestUsers.CreateActive("+905552222222", time.GetUtcNow());

        var club = Organization.Create(
            founder.Id, "Özel Kulüp", null, null, "ABCD2345", false, time.GetUtcNow());
        var membership = OrganizationMember.CreateFounder(club.Id, founder.Id, time.GetUtcNow());
        var openEvent = CreateEvent(founder.Id, sport.Id, "Açık etkinlik", club.Id, false, time);
        var membersOnlyEvent = CreateEvent(founder.Id, sport.Id, "Üyelere özel", club.Id, true, time);

        db.Users.AddRange(viewer, founder);
        db.Sports.Add(sport);
        db.Organizations.Add(club);
        db.OrganizationMembers.Add(membership);
        db.Events.AddRange(openEvent, membersOnlyEvent);
        await db.SaveChangesAsync();

        var handler = new ListOrganizationEventsQueryHandler(db, new TestCurrentUser(viewer.Id), time);
        var result = await handler.Handle(new ListOrganizationEventsQuery(club.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle(item => item.Id == openEvent.Id);
    }

    [Fact]
    public async Task Handle_MissingClub_ReturnsNotFound()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var viewer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        db.Users.Add(viewer);
        await db.SaveChangesAsync();

        var handler = new ListOrganizationEventsQueryHandler(db, new TestCurrentUser(viewer.Id), time);
        var result = await handler.Handle(new ListOrganizationEventsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == "Organization.NotFound");
    }

    private static DomainEvent CreateEvent(
        Guid organizerId,
        Guid sportId,
        string title,
        Guid organizationId,
        bool isOrganizationOnly,
        FakeTimeProvider time)
    {
        var @event = DomainEvent.Create(
            organizerId,
            sportId,
            title,
            time.GetUtcNow().AddDays(1),
            90,
            41m,
            29m,
            "İstanbul",
            time.GetUtcNow(),
            maxParticipants: 10,
            organizationId: organizationId,
            isOrganizationOnly: isOrganizationOnly);
        @event.Publish(time.GetUtcNow());
        return @event;
    }
}
