using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Gamification;
using Sportner.Application.Features.Identity.UserSports.AddSport;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Identity.UserSports;

public sealed class AddSportCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_AddsPrimarySport_ForExistingUser()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(UtcNow);

        var user = TestUsers.CreateActive("+905559999001", UtcNow);
        var sport = Sport.Create("Basketbol", 1, UtcNow, slug: "basketbol");
        db.Users.Add(user);
        db.Sports.Add(sport);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, time, user.Id);
        var result = await handler.Handle(
            new AddSportCommand(sport.Id, SkillLevel: 3, IsPrimary: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Value.Should().HaveCount(1);
        result.Value![0].SportId.Should().Be(sport.Id);
        result.Value[0].SkillLevel.Should().Be(3);
        result.Value[0].IsPrimary.Should().BeTrue();

        db.ChangeTracker.Clear();
        var persisted = await db.UserSports.AsNoTracking().SingleAsync();
        persisted.UserId.Should().Be(user.Id);
        persisted.SportId.Should().Be(sport.Id);
        persisted.IsPrimary.Should().BeTrue();
        persisted.SkillLevel.Should().Be(SkillLevel.Expert);
    }

    [Fact]
    public async Task Handle_ReloadsViaInclude_ThenAddsSecondSport()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(UtcNow);

        var user = TestUsers.CreateActive("+905559999002", UtcNow);
        var first = Sport.Create("Futbol", 2, UtcNow, slug: "futbol");
        var second = Sport.Create("Tenis", 4, UtcNow, slug: "tenis");
        user.AddSport(first.Id, SkillLevel.Beginner, UtcNow, isPrimary: true);
        db.Users.Add(user);
        db.Sports.AddRange(first, second);
        await db.SaveChangesAsync();

        // Detach so the next load goes through Include + AsReadOnly navigation.
        db.ChangeTracker.Clear();

        var handler = CreateHandler(db, time, user.Id);
        var result = await handler.Handle(
            new AddSportCommand(second.Id, SkillLevel: 1, IsPrimary: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Value.Should().HaveCount(2);
    }

    private static AddSportCommandHandler CreateHandler(
        AppDbContext db,
        TimeProvider time,
        Guid userId)
    {
        var notifications = new Mock<INotificationPublisher>();
        var badgeAwarder = new BadgeAwarder(
            db,
            notifications.Object,
            time,
            NullLogger<BadgeAwarder>.Instance);

        return new AddSportCommandHandler(
            db,
            new TestCurrentUser(userId),
            time,
            badgeAwarder);
    }
}
