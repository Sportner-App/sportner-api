using System.Net;
using FluentAssertions;
using Moq;
using Sportner.Application.Abstractions;
using Sportner.Application.DTOs.Auth;
using Sportner.Application.Services;
using Sportner.Domain.Abstractions;
using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;
using Sportner.Domain.Exceptions;

namespace Sportner.Application.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IProfileRepository> _profiles = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    public AuthServiceTests()
    {
        _uow.SetupGet(x => x.Profiles).Returns(_profiles.Object);
        _tokens.Setup(x => x.CreateToken(It.IsAny<Profile>())).Returns("jwt-token");
    }

    private AuthService Sut() => new(_uow.Object, _tokens.Object, _currentUser.Object);

    [Fact]
    public async Task Login_InvalidPassword_ThrowsBadRequest()
    {
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"),
            FullName = "User"
        };

        _profiles
            .Setup(p => p.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Profile, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var act = () => Sut().LoginAsync(new LoginDto("user@example.com", "wrong"));

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"),
            FullName = "User"
        };

        _profiles
            .Setup(p => p.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Profile, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await Sut().LoginAsync(new LoginDto("user@example.com", "correct"));

        result.Token.Should().Be("jwt-token");
        result.UserId.Should().Be(profile.Id);
        result.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Register_ExistingWithPassword_Throws()
    {
        var existing = new Profile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = "hash"
        };

        _profiles
            .Setup(p => p.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Profile, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => Sut().RegisterAsync(new RegisterDto("user@example.com", "password1", "Name"));

        await act.Should().ThrowAsync<ApiException>()
            .Where(e => e.HttpStatusCode == HttpStatusCode.BadRequest);
    }
}
