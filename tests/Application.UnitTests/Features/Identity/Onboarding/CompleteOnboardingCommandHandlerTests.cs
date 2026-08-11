using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.Onboarding.CompleteOnboarding;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Identity.Onboarding;

public sealed class CompleteOnboardingCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Fails_WhenProfileIsMissing()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(UtcNow);

        var user = TestUsers.CreateActive("+905551111111", UtcNow);
        user.AddSport(Guid.NewGuid(), SkillLevel.Beginner, UtcNow);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await CreateHandler(db, time, user.Id)
            .Handle(new CompleteOnboardingCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Onboarding.ProfileRequired");
    }

    [Fact]
    public async Task Handle_Fails_WhenNoSportSelected()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(UtcNow);

        var user = TestUsers.CreateActive("+905551111111", UtcNow);
        user.AttachUserProfile(UserProfile.Create(user.Id, "username", "First", UtcNow));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await CreateHandler(db, time, user.Id)
            .Handle(new CompleteOnboardingCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Onboarding.SportRequired");
    }

    [Fact]
    public async Task Handle_StampsCompletionDate_WhenProfileAndSportExist()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(UtcNow);

        var user = TestUsers.CreateActive("+905551111111", UtcNow);
        user.AttachUserProfile(UserProfile.Create(user.Id, "username", "First", UtcNow));
        user.AddSport(Guid.NewGuid(), SkillLevel.Beginner, UtcNow);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await CreateHandler(db, time, user.Id)
            .Handle(new CompleteOnboardingCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.OnboardingCompletedAt.Should().Be(UtcNow);
    }

    private static CompleteOnboardingCommandHandler CreateHandler(
        AppDbContext db,
        TimeProvider time,
        Guid userId)
    {
        return new CompleteOnboardingCommandHandler(db, new TestCurrentUser(userId), time);
    }
}
