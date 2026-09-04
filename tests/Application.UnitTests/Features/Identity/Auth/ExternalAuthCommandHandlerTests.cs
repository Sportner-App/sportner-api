using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Identity.Auth.CompleteExternalRegistration;
using Sportner.Application.Features.Identity.Auth.SignInWithGoogle;
using Sportner.Application.Features.Identity.Auth.SignInWithApple;
using Sportner.Application.Features.Identity.Auth.RefreshToken;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Infrastructure.Authentication;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class ExternalAuthCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 9, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FirstGoogleSignIn_ReturnsRegistrationTicket_WithoutCreatingUser()
    {
        await using var db = InMemoryDb.Create();
        var verifier = GoogleVerifier();
        var ticketService = TicketService();
        var handler = new SignInWithGoogleCommandHandler(
            db, verifier.Object, Jwt().Object, TokenHasher().Object, ticketService.Object, Time());

        var result = await handler.Handle(
            new SignInWithGoogleCommand("provider-token", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequiresRegistration.Should().BeTrue();
        result.Value.RegistrationToken.Should().Be("registration-token");
        db.Users.Should().BeEmpty();
        db.UserProfiles.Should().BeEmpty();
    }

    [Fact]
    public async Task FirstAppleSignIn_PreservesOneTimeNameInRegistrationTicket()
    {
        await using var db = InMemoryDb.Create();
        var verifier = new Mock<IAppleTokenVerifier>();
        verifier.Setup(service => service.VerifyAsync("apple-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalIdentity>.Success(
                new ExternalIdentity("apple-sub", "private@privaterelay.appleid.com")));
        ExternalRegistrationTicket? capturedTicket = null;
        var ticketService = TicketService();
        ticketService.Setup(candidate => candidate.Create(It.IsAny<ExternalRegistrationTicket>()))
            .Callback<ExternalRegistrationTicket>(ticket => capturedTicket = ticket)
            .Returns(new ExternalRegistrationToken("registration-token", UtcNow.AddMinutes(15)));
        var handler = new SignInWithAppleCommandHandler(
            db, verifier.Object, Jwt().Object, TokenHasher().Object, ticketService.Object, Time());

        var result = await handler.Handle(new SignInWithAppleCommand(
            "apple-token", "Yağız", "Erdenler", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequiresRegistration.Should().BeTrue();
        capturedTicket!.FirstName.Should().Be("Yağız");
        capturedTicket.LastName.Should().Be("Erdenler");
        db.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteRegistration_CreatesUserProfileExternalLoginAndSessionAtomically()
    {
        await using var db = InMemoryDb.Create();
        var ticketService = TicketService();
        var handler = new CompleteExternalRegistrationCommandHandler(
            db, ticketService.Object, Jwt().Object, TokenHasher().Object, Time());

        var result = await handler.Handle(new CompleteExternalRegistrationCommand(
            "registration-token", "yagizerdenler", "Yağız", "Erdenler",
            new DateOnly(1995, 1, 1), 0, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Users.Should().ContainSingle();
        db.UserProfiles.Should().ContainSingle(profile =>
            profile.Username == "yagizerdenler" &&
            profile.BirthDate == new DateOnly(1995, 1, 1) &&
            profile.ProfileImageUrl == "https://example.com/avatar.jpg");
        db.UserExternalLogins.Should().ContainSingle();
        db.UserSessions.Should().ContainSingle();
    }

    [Fact]
    public async Task ReturningGoogleUser_CanSignInAfterDbContextReload()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using (var creationDb = InMemoryDb.Create(databaseName))
        {
            var complete = new CompleteExternalRegistrationCommandHandler(
                creationDb, TicketService().Object, Jwt().Object, TokenHasher().Object, Time());
            (await complete.Handle(new CompleteExternalRegistrationCommand(
                "registration-token", "returninguser", "Returning", null,
                new DateOnly(1990, 1, 1), 0, null, null), CancellationToken.None))
                .IsSuccess.Should().BeTrue();
        }

        await using var signInDb = InMemoryDb.Create(databaseName);
        var handler = new SignInWithGoogleCommandHandler(
            signInDb, GoogleVerifier().Object, Jwt().Object, TokenHasher().Object,
            TicketService().Object, Time());

        var result = await handler.Handle(
            new SignInWithGoogleCommand("provider-token", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequiresRegistration.Should().BeFalse();
        result.Value.Authentication.Should().NotBeNull();
    }

    [Fact]
    public async Task RegistrationTicket_IsSignedAndExpiresAfterFifteenMinutes()
    {
        var time = new FakeTimeProvider(UtcNow);
        var service = new ExternalRegistrationTokenService(
            Options.Create(new JwtSettings
            {
                Secret = "a-test-secret-that-is-long-enough-for-hmac-sha256",
                Issuer = "SportnerTests",
                Audience = "SportnerMobile"
            }),
            time);
        var ticket = new ExternalRegistrationTicket(
            ExternalLoginProvider.Google, "google-sub", "user@example.com",
            "Yağız", "Erdenler", "https://example.com/avatar.jpg");

        var token = service.Create(ticket);
        var valid = await service.ValidateAsync(token.Token, CancellationToken.None);
        valid.IsSuccess.Should().BeTrue();
        valid.Value.Should().Be(ticket);

        time.Advance(TimeSpan.FromMinutes(16));
        var expired = await service.ValidateAsync(token.Token, CancellationToken.None);
        expired.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExternalUser_CanRefreshSessionAfterDbContextReload()
    {
        await using var db = InMemoryDb.Create();
        var complete = new CompleteExternalRegistrationCommandHandler(
            db, TicketService().Object, Jwt().Object, TokenHasher().Object, Time());
        (await complete.Handle(new CompleteExternalRegistrationCommand(
            "registration-token", "refreshuser", "Refresh", null,
            new DateOnly(1990, 1, 1), 0, null, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        db.ChangeTracker.Clear();

        var handler = new RefreshTokenCommandHandler(
            db, Jwt().Object, TokenHasher().Object, Time());
        var result = await handler.Handle(
            new RefreshTokenCommand("refresh"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    private static Mock<IGoogleTokenVerifier> GoogleVerifier()
    {
        var verifier = new Mock<IGoogleTokenVerifier>();
        verifier.Setup(service => service.VerifyAsync("provider-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalIdentity>.Success(new ExternalIdentity(
                "google-sub", "user@example.com", "Yağız", "Erdenler", "https://example.com/avatar.jpg")));
        return verifier;
    }

    private static Mock<IExternalRegistrationTokenService> TicketService()
    {
        var data = new ExternalRegistrationTicket(
            ExternalLoginProvider.Google, "google-sub", "user@example.com",
            "Yağız", "Erdenler", "https://example.com/avatar.jpg");
        var service = new Mock<IExternalRegistrationTokenService>();
        service.Setup(candidate => candidate.Create(It.IsAny<ExternalRegistrationTicket>()))
            .Returns(new ExternalRegistrationToken("registration-token", UtcNow.AddMinutes(15)));
        service.Setup(candidate => candidate.ValidateAsync("registration-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalRegistrationTicket>.Success(data));
        return service;
    }

    private static Mock<IJwtService> Jwt()
    {
        var service = new Mock<IJwtService>();
        service.Setup(candidate => candidate.CreateAccessToken(It.IsAny<Guid>(), null))
            .Returns(new AccessToken("access", UtcNow.AddHours(1)));
        service.Setup(candidate => candidate.GenerateRefreshToken())
            .Returns(new RefreshToken("refresh", UtcNow.AddDays(30)));
        return service;
    }

    private static Mock<ITokenHasher> TokenHasher()
    {
        var service = new Mock<ITokenHasher>();
        service.Setup(candidate => candidate.Hash("refresh")).Returns("refresh-hash");
        return service;
    }

    private static TimeProvider Time()
    {
        var provider = new Mock<TimeProvider>();
        provider.Setup(candidate => candidate.GetUtcNow()).Returns(UtcNow);
        return provider.Object;
    }
}
