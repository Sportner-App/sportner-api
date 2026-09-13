using FluentAssertions;
using Sportner.Application.Features.Identity.Auth.UpdatePreferredLanguage;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class UpdatePreferredLanguageCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesPreferredLanguage_ForAuthenticatedUser()
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;
        var user = TestUsers.CreateActive("+905551110099", now);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new UpdatePreferredLanguageCommandHandler(
            db,
            new TestCurrentUser(user.Id),
            TimeProvider.System);

        var result = await handler.Handle(
            new UpdatePreferredLanguageCommand(Language.English),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Users.Single(candidate => candidate.Id == user.Id).PreferredLanguage
            .Should().Be(Language.English);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotAuthenticated()
    {
        await using var db = InMemoryDb.Create();

        var handler = new UpdatePreferredLanguageCommandHandler(
            db,
            new TestCurrentUser(null),
            TimeProvider.System);

        var result = await handler.Handle(
            new UpdatePreferredLanguageCommand(Language.English),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
