using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Admin.Clubs;
using Sportner.Application.Features.Admin.Stats;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Domain.Organizations;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Admin;

public sealed class AdminStatsAndClubTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));

    private User NewUser(string email) =>
        User.RegisterWithExternalProvider(ExternalLoginProvider.Google, Guid.NewGuid().ToString(), email, _time.GetUtcNow());

    [Fact]
    public async Task Stats_ExcludesTestAccounts_AndReturnsThirtySignupDays()
    {
        await using var db = InMemoryDb.Create();
        var real = NewUser("real@example.com");
        var test = NewUser("test@example.com");
        test.MarkAsTestAccount();
        db.Users.AddRange(real, test);
        await db.SaveChangesAsync();

        var handler = new GetAdminStatsQueryHandler(db, _time);
        var result = await handler.Handle(new GetAdminStatsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        stats.Users.Total.Should().Be(1);
        stats.Users.New24h.Should().Be(1);
        stats.SignupsByDay.Should().HaveCount(30);
        stats.SignupsByDay.Sum(day => day.Count).Should().Be(1);
        stats.SignupsByDay[^1].Date.Should().Be("2026-11-18");
        stats.SignupsByDay[0].Date.Should().Be("2026-10-20");
    }

    [Fact]
    public async Task Close_CancelsUpcomingEvents_AndWritesAudit()
    {
        await using var db = InMemoryDb.Create();
        var now = _time.GetUtcNow();
        var admin = NewUser("boss@example.com");
        var founder = NewUser("founder@example.com");
        var club = Organization.Create(founder.Id, "Kulüp", null, null, "ABCD2345", true, now);
        var upcoming = Event.Create(
            founder.Id, Guid.NewGuid(), "Yaklaşan", now.AddDays(3), 60, 41m, 29m, "Adres", now,
            organizationId: club.Id);
        upcoming.Publish(now);
        var past = Event.Create(
            founder.Id, Guid.NewGuid(), "Geçmiş", now.AddDays(-3), 60, 41m, 29m, "Adres", now.AddDays(-10),
            organizationId: club.Id);
        db.Users.AddRange(admin, founder);
        db.Organizations.Add(club);
        db.Events.AddRange(upcoming, past);
        await db.SaveChangesAsync();

        var handler = new CloseClubCommandHandler(
            db, new TestCurrentUser(admin.Id), _time, new Mock<INotificationPublisher>().Object);
        (await handler.Handle(new CloseClubCommand(club.Id), default)).IsSuccess.Should().BeTrue();

        (await db.Organizations.IgnoreQueryFilters().SingleAsync()).DeletedAt.Should().NotBeNull();
        (await db.Events.SingleAsync(e => e.Id == upcoming.Id)).Status.Should().Be(EventStatus.Cancelled);
        (await db.Events.SingleAsync(e => e.Id == past.Id)).Status.Should().NotBe(EventStatus.Cancelled);
        var entry = await db.AdminAuditEntries.SingleAsync();
        entry.Action.Should().Be("club.close");
        entry.TargetType.Should().Be("club");
        entry.TargetId.Should().Be(club.Id);
        entry.Details.Should().Be("1");

        (await handler.Handle(new CloseClubCommand(club.Id), default)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Visibility_IsIdempotent_AndAuditsOnlyOnChange()
    {
        await using var db = InMemoryDb.Create();
        var now = _time.GetUtcNow();
        var admin = NewUser("boss@example.com");
        var club = Organization.Create(Guid.NewGuid(), "Kulüp", null, null, "ABCD2345", true, now);
        db.Users.Add(admin);
        db.Organizations.Add(club);
        await db.SaveChangesAsync();

        var handler = new SetClubVisibilityCommandHandler(db, new TestCurrentUser(admin.Id), _time);

        (await handler.Handle(new SetClubVisibilityCommand(club.Id, true), default)).IsSuccess.Should().BeTrue();
        (await db.AdminAuditEntries.CountAsync()).Should().Be(0);

        (await handler.Handle(new SetClubVisibilityCommand(club.Id, false), default)).IsSuccess.Should().BeTrue();
        (await handler.Handle(new SetClubVisibilityCommand(club.Id, false), default)).IsSuccess.Should().BeTrue();

        (await db.Organizations.SingleAsync()).IsPublic.Should().BeFalse();
        var entry = await db.AdminAuditEntries.SingleAsync();
        entry.Action.Should().Be("club.visibility");
        entry.Details.Should().Be("private");
    }
}
