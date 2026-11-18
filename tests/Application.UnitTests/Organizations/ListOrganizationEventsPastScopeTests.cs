using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Organizations;
using Sportner.Application.Features.Organizations.ListOrganizationEvents;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Organizations;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Organizations;

public sealed class ListOrganizationEventsPastScopeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private sealed record Seeded(
        string DatabaseName,
        User Founder,
        User Outsider,
        Organization Club,
        DomainEvent CompletedOld,
        DomainEvent CompletedRecent,
        DomainEvent StartedNotCompleted,
        DomainEvent MembersOnlyCompleted,
        DomainEvent Cancelled,
        DomainEvent Draft,
        DomainEvent Upcoming);

    private static DomainEvent Create(
        Guid creator, Guid sportId, string title, Guid clubId, DateTimeOffset start, bool membersOnly = false)
    {
        // Geçmişe taşıyabilmek için oluşturma anı başlangıçtan önce.
        var created = start.AddDays(-2);
        return DomainEvent.Create(
            creator, sportId, title, start, 60, 41m, 29m, "İstanbul", created,
            maxParticipants: 10, organizationId: clubId, isOrganizationOnly: membersOnly);
    }

    private static async Task<Seeded> SeedAsync()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var founder = TestUsers.CreateActive("+905551111111", Now);
        var outsider = TestUsers.CreateActive("+905552222222", Now);
        var sport = Sport.Create("Futbol", 1, Now, "futbol");
        var club = Organization.Create(founder.Id, "Kulüp", null, null, "ABCD2345", true, Now);

        var completedOld = Create(founder.Id, sport.Id, "Eski", club.Id, Now.AddDays(-20));
        completedOld.Publish(Now.AddDays(-22));
        completedOld.Complete(Now.AddDays(-19));

        var completedRecent = Create(founder.Id, sport.Id, "Yeni", club.Id, Now.AddDays(-3));
        completedRecent.Publish(Now.AddDays(-5));
        completedRecent.Complete(Now.AddDays(-2));

        var started = Create(founder.Id, sport.Id, "Başladı", club.Id, Now.AddHours(-1));
        started.Publish(Now.AddDays(-3));

        var membersOnly = Create(founder.Id, sport.Id, "Üyelere özel", club.Id, Now.AddDays(-10), membersOnly: true);
        membersOnly.Publish(Now.AddDays(-12));
        membersOnly.Complete(Now.AddDays(-9));

        var cancelled = Create(founder.Id, sport.Id, "İptal", club.Id, Now.AddDays(-4));
        cancelled.Publish(Now.AddDays(-6));
        cancelled.Cancel(Now.AddDays(-5));

        var draft = Create(founder.Id, sport.Id, "Taslak", club.Id, Now.AddDays(-1));

        var upcoming = Create(founder.Id, sport.Id, "Yaklaşan", club.Id, Now.AddDays(3));
        upcoming.Publish(Now);

        await using var seed = InMemoryDb.Create(databaseName);
        seed.Users.AddRange(founder, outsider);
        seed.Sports.Add(sport);
        seed.Organizations.Add(club);
        seed.OrganizationMembers.Add(OrganizationMember.CreateFounder(club.Id, founder.Id, Now));
        seed.Events.AddRange(completedOld, completedRecent, started, membersOnly, cancelled, draft, upcoming);
        await seed.SaveChangesAsync();

        return new Seeded(
            databaseName, founder, outsider, club,
            completedOld, completedRecent, started, membersOnly, cancelled, draft, upcoming);
    }

    private static async Task<IReadOnlyList<EventListItemResponse>> ListAsync(
        Seeded seeded, Guid viewer, string? scope, int page = 1, int pageSize = 20)
    {
        await using var db = InMemoryDb.Create(seeded.DatabaseName);
        var handler = new ListOrganizationEventsQueryHandler(
            db, new TestCurrentUser(viewer), new FakeTimeProvider(Now));
        var result = await handler.Handle(
            new ListOrganizationEventsQuery(seeded.Club.Id, scope, page, pageSize), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    [Fact]
    public async Task PastScope_ReturnsCompletedAndStartedEvents_NewestFirst_ForMember()
    {
        var seeded = await SeedAsync();

        var items = await ListAsync(seeded, seeded.Founder.Id, "past");

        items.Select(item => item.Title).Should().Equal("Başladı", "Yeni", "Üyelere özel", "Eski");
    }

    [Fact]
    public async Task PastScope_HidesMembersOnlyEvents_FromNonMembers()
    {
        var seeded = await SeedAsync();

        var items = await ListAsync(seeded, seeded.Outsider.Id, "past");

        items.Select(item => item.Title).Should().Equal("Başladı", "Yeni", "Eski");
    }

    [Fact]
    public async Task PastScope_Pages()
    {
        var seeded = await SeedAsync();

        var first = await ListAsync(seeded, seeded.Founder.Id, "past", page: 1, pageSize: 3);
        var second = await ListAsync(seeded, seeded.Founder.Id, "past", page: 2, pageSize: 3);

        first.Select(item => item.Title).Should().Equal("Başladı", "Yeni", "Üyelere özel");
        second.Select(item => item.Title).Should().Equal("Eski");
    }

    [Fact]
    public async Task DefaultScope_KeepsUpcomingBehavior()
    {
        var seeded = await SeedAsync();

        var items = await ListAsync(seeded, seeded.Founder.Id, scope: null);

        // Yaklaşan + son 24 saatte başlayanlar (önceki davranış); tamamlanmış eski etkinlikler yok.
        items.Select(item => item.Title).Should().Contain("Yaklaşan").And.NotContain("Eski");
        items.Should().BeInAscendingOrder(item => item.EventDate);
        items.Should().OnlyContain(item => item.OrganizationName == "Kulüp");
    }

    [Fact]
    public async Task OrganizationDetail_CountsCompletedClubEvents()
    {
        var seeded = await SeedAsync();
        await using var db = InMemoryDb.Create(seeded.DatabaseName);

        var detail = await OrganizationQueries.GetDetailAsync(
            db, seeded.Club.Id, seeded.Founder.Id, CancellationToken.None);
        var publicDetail = await OrganizationQueries.GetDetailAsync(
            db, seeded.Club.Id, seeded.Outsider.Id, CancellationToken.None);

        // Eski + Yeni + Üyelere özel (yalnızca bir sayı olduğu için görünürlük süzülmez).
        detail!.CompletedEventCount.Should().Be(3);
        publicDetail!.CompletedEventCount.Should().Be(3);
    }

    [Fact]
    public void ListItem_ProjectionIncludesClubImageAndOrganizerFlag_AndPastQueryTranslatesToSql()
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
                && @event.Status != EventStatus.Cancelled)
            .Where(@event =>
                @event.Status == EventStatus.Completed
                || (@event.Status != EventStatus.Draft && @event.EventDate <= utcNow))
            .OrderByDescending(@event => @event.EventDate)
            .ThenBy(@event => @event.Id)
            .Skip(20)
            .Take(20);

        var query = EventQueries.ProjectListItems(dbContext, events);

        var act = () => query.ToQueryString();
        act.Should().NotThrow();
        query.ToQueryString().Should().Contain("ImageUrl").And.Contain("IsOrganizerParticipant");
    }

    [Fact]
    public async Task Validator_RejectsUnknownScopeAndOutOfRangePaging()
    {
        var validator = new ListOrganizationEventsQueryValidator();
        var id = Guid.NewGuid();

        (await validator.ValidateAsync(new ListOrganizationEventsQuery(id, "foo"))).IsValid.Should().BeFalse();
        (await validator.ValidateAsync(new ListOrganizationEventsQuery(id, "past", 1, 51))).IsValid.Should().BeFalse();
        (await validator.ValidateAsync(new ListOrganizationEventsQuery(id, "past", 0, 20))).IsValid.Should().BeFalse();
        (await validator.ValidateAsync(new ListOrganizationEventsQuery(id, "PAST"))).IsValid.Should().BeTrue();
        (await validator.ValidateAsync(new ListOrganizationEventsQuery(id))).IsValid.Should().BeTrue();
    }
}
