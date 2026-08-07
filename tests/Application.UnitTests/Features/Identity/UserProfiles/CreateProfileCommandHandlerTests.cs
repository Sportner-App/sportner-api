using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.UserProfiles.CreateProfile;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Identity.UserProfiles;

public sealed class CreateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_Fails_WhenUsernameAlreadyTaken()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));

        var owner = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var challenger = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var existing = UserProfile.Create(owner.Id, "taken_name", "Owner", time.GetUtcNow());
        owner.AttachUserProfile(existing);

        db.Users.AddRange(owner, challenger);
        await db.SaveChangesAsync();

        var handler = new CreateProfileCommandHandler(
            db,
            new TestCurrentUser(challenger.Id),
            time);

        var result = await handler.Handle(
            new CreateProfileCommand(
                "Taken_Name",
                "Challenger",
                LastName: null,
                Bio: null,
                City: null,
                IsProfilePublic: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Profile.UsernameTaken");
    }
}
