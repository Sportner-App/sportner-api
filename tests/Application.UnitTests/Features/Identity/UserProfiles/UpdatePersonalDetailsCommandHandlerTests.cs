using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.UserProfiles.UpdatePersonalDetails;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Identity.UserProfiles;

public sealed class UpdatePersonalDetailsCommandHandlerTests
{
    [Fact]
    public async Task Handle_SetsBirthDate_WhenNotPreviouslySet()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));

        var user = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var profile = UserProfile.Create(user.Id, "someone", "Someone", time.GetUtcNow());
        user.AttachUserProfile(profile);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new UpdatePersonalDetailsCommandHandler(db, new TestCurrentUser(user.Id), time);

        var result = await handler.Handle(
            new UpdatePersonalDetailsCommand(1, new DateOnly(1995, 1, 1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Value!.BirthDate.Should().Be(new DateOnly(1995, 1, 1));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenResendingTheSameBirthDate()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));

        var user = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var profile = UserProfile.Create(user.Id, "someone", "Someone", time.GetUtcNow());
        profile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), time.GetUtcNow());
        user.AttachUserProfile(profile);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new UpdatePersonalDetailsCommandHandler(db, new TestCurrentUser(user.Id), time);

        // Editing gender alone still resends the unchanged birth date from the client.
        var result = await handler.Handle(
            new UpdatePersonalDetailsCommand(2, new DateOnly(1995, 1, 1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Value!.Gender.Should().Be(2);
        result.Value!.BirthDate.Should().Be(new DateOnly(1995, 1, 1));
    }

    [Fact]
    public async Task Handle_Fails_WhenChangingAnAlreadySetBirthDate()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));

        var user = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var profile = UserProfile.Create(user.Id, "someone", "Someone", time.GetUtcNow());
        profile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), time.GetUtcNow());
        user.AttachUserProfile(profile);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new UpdatePersonalDetailsCommandHandler(db, new TestCurrentUser(user.Id), time);

        // Trying to shave years off to sneak into an age-restricted event.
        var result = await handler.Handle(
            new UpdatePersonalDetailsCommand(1, new DateOnly(2008, 1, 1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Profile.BirthDateLocked");
    }
}
