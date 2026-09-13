using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Features.Identity.Auth.VerifyEmail;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class VerifyEmailCommandHandlerTests
{
    private sealed class FakeTokenHasher : ITokenHasher
    {
        public string Hash(string value) => $"hash:{value}";

        public bool Verify(string value, string hash) => hash == $"hash:{value}";
    }

    [Fact]
    public async Task Handle_ConfirmsEmail_WhenCodeMatchesAndIsNotExpired()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));
        var hasher = new FakeTokenHasher();

        var user = User.RegisterWithPassword("hash", "someone@example.com", time.GetUtcNow());
        user.IssueEmailVerificationCode(
            hasher.Hash("123456"),
            time.GetUtcNow().AddMinutes(15),
            time.GetUtcNow());

        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new VerifyEmailCommandHandler(db, new TestCurrentUser(user.Id), hasher, time);

        var result = await handler.Handle(new VerifyEmailCommand("123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));

        var stored = await db.Users.FindAsync(user.Id);
        stored!.EmailVerifiedAt.Should().Be(time.GetUtcNow());
        stored.EmailVerificationCodeHash.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeIsWrong()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));
        var hasher = new FakeTokenHasher();

        var user = User.RegisterWithPassword("hash", "someone@example.com", time.GetUtcNow());
        user.IssueEmailVerificationCode(
            hasher.Hash("123456"),
            time.GetUtcNow().AddMinutes(15),
            time.GetUtcNow());

        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new VerifyEmailCommandHandler(db, new TestCurrentUser(user.Id), hasher, time);

        var result = await handler.Handle(new VerifyEmailCommand("000000"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "Auth.EmailVerificationCodeInvalid");
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeExpired()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));
        var hasher = new FakeTokenHasher();

        var user = User.RegisterWithPassword("hash", "someone@example.com", time.GetUtcNow());
        user.IssueEmailVerificationCode(
            hasher.Hash("123456"),
            time.GetUtcNow().AddMinutes(15),
            time.GetUtcNow());

        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        time.Advance(TimeSpan.FromMinutes(16));

        var handler = new VerifyEmailCommandHandler(db, new TestCurrentUser(user.Id), hasher, time);

        var result = await handler.Handle(new VerifyEmailCommand("123456"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "Auth.EmailVerificationCodeExpired");
    }
}
