using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Gamification;
using Sportner.Application.Features.Identity.UserSports.AddSports;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Identity.UserSports;

public sealed class AddSportsCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_AddsAllSportsWithOneCommand()
    {
        await using var db = InMemoryDb.Create();
        var user = TestUsers.CreateActive("+905559999010", UtcNow);
        var basketball = Sport.Create("Basketbol", 1, UtcNow, slug: "basketbol");
        var tennis = Sport.Create("Tenis", 2, UtcNow, slug: "tenis");
        db.Users.Add(user);
        db.Sports.AddRange(basketball, tennis);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(
            new AddSportsCommand([
                new AddSportsItem(basketball.Id, 1),
                new AddSportsItem(tennis.Id, 3),
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        var persisted = await db.UserSports.AsNoTracking().ToListAsync();
        persisted.Should().HaveCount(2);
        persisted.Single(item => item.SportId == basketball.Id).SkillLevel
            .Should().Be(SkillLevel.Intermediate);
        persisted.Single(item => item.SportId == tennis.Id).SkillLevel
            .Should().Be(SkillLevel.Expert);
    }

    [Fact]
    public async Task Handle_DoesNotAddAnySport_WhenOneAlreadyExists()
    {
        await using var db = InMemoryDb.Create();
        var user = TestUsers.CreateActive("+905559999011", UtcNow);
        var existing = Sport.Create("Futbol", 1, UtcNow, slug: "futbol");
        var newSport = Sport.Create("Yüzme", 2, UtcNow, slug: "yuzme");
        user.AddSport(existing.Id, SkillLevel.Beginner, UtcNow, isPrimary: true);
        db.Users.Add(user);
        db.Sports.AddRange(existing, newSport);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(
            new AddSportsCommand([
                new AddSportsItem(existing.Id, 2),
                new AddSportsItem(newSport.Id, 2),
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await db.UserSports.AsNoTracking().CountAsync()).Should().Be(1);
    }

    private static AddSportsCommandHandler CreateHandler(AppDbContext db, Guid userId)
    {
        var time = new FakeTimeProvider(UtcNow);
        var notifications = new Mock<INotificationPublisher>();
        var badgeAwarder = new BadgeAwarder(
            db,
            notifications.Object,
            time,
            NullLogger<BadgeAwarder>.Instance);

        return new AddSportsCommandHandler(db, new TestCurrentUser(userId), time, badgeAwarder);
    }
}
