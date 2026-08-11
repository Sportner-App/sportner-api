using FluentAssertions;
using Moq;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Features.Identity.Auth.Login;
using Sportner.Application.Features.Identity.Auth.Register;
using Sportner.Application.UnitTests.Infrastructure;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class PasswordAuthCommandHandlerTests
{
    [Fact]
    public async Task Register_CreatesUserAndReturnsTokens()
    {
        await using var db = InMemoryDb.Create();
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(h => h.Hash("Password1!")).Returns("hashed");

        var jwt = CreateJwtMock();
        var tokenHasher = new Mock<ITokenHasher>();
        tokenHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("refresh-hash");

        var handler = new RegisterCommandHandler(
            db,
            passwordHasher.Object,
            jwt.Object,
            tokenHasher.Object,
            TimeProvider.System);

        var result = await handler.Handle(
            new RegisterCommand("AhmetX", "Password1!", "Ahmet", "Yilmaz", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsNewUser.Should().BeTrue();
        db.Users.Should().ContainSingle();
        db.UserProfiles.Should().ContainSingle(p => p.Username == "ahmetx");
    }

    [Fact]
    public async Task Login_WithValidCredentials_Succeeds()
    {
        await using var db = InMemoryDb.Create();
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        passwordHasher.Setup(h => h.Verify("hashed", "Password1!")).Returns(true);

        var jwt = CreateJwtMock();
        var tokenHasher = new Mock<ITokenHasher>();
        tokenHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("refresh-hash");

        var register = new RegisterCommandHandler(
            db,
            passwordHasher.Object,
            jwt.Object,
            tokenHasher.Object,
            TimeProvider.System);

        await register.Handle(
            new RegisterCommand("player1", "Password1!", "Player", null, null, null),
            CancellationToken.None);

        db.ChangeTracker.Clear();

        var login = new LoginCommandHandler(
            db,
            passwordHasher.Object,
            jwt.Object,
            tokenHasher.Object,
            TimeProvider.System);

        var result = await login.Handle(
            new LoginCommand("Player1", "Password1!", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsNewUser.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Fails()
    {
        await using var db = InMemoryDb.Create();
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        passwordHasher.Setup(h => h.Verify("hashed", "wrong")).Returns(false);

        var jwt = CreateJwtMock();
        var tokenHasher = new Mock<ITokenHasher>();
        tokenHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("refresh-hash");

        var register = new RegisterCommandHandler(
            db,
            passwordHasher.Object,
            jwt.Object,
            tokenHasher.Object,
            TimeProvider.System);

        await register.Handle(
            new RegisterCommand("player2", "Password1!", "Player", null, null, null),
            CancellationToken.None);

        var login = new LoginCommandHandler(
            db,
            passwordHasher.Object,
            jwt.Object,
            tokenHasher.Object,
            TimeProvider.System);

        var result = await login.Handle(
            new LoginCommand("player2", "wrong", null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Auth.InvalidCredentials");
    }

    private static Mock<IJwtService> CreateJwtMock()
    {
        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.CreateAccessToken(It.IsAny<Guid>()))
            .Returns(new AccessToken("access", DateTimeOffset.UtcNow.AddHours(1)));
        jwt.Setup(j => j.GenerateRefreshToken())
            .Returns(new RefreshToken("refresh", DateTimeOffset.UtcNow.AddDays(30)));
        return jwt;
    }
}
