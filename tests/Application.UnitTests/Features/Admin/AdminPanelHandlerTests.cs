using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Email;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Features.Admin.Auth;
using Sportner.Application.Features.Admin.Feedback;
using Sportner.Application.Features.Admin.Users;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Feedback;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Admin;

public sealed class AdminPanelHandlerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero));

    private sealed class Admins(params Guid[] ids) : IAdminDirectory
    {
        public bool IsAdmin(Guid userId) => ids.Contains(userId);
    }

    /// <summary>Gerçek özet yerine düz metin: test kodu doğrudan karşılaştırabilsin.</summary>
    private sealed class PlainHasher : ITokenHasher
    {
        public string Hash(string value) => "h:" + value;

        public bool Verify(string value, string hash) => Hash(value) == hash;
    }

    private User ExternalUser(string email) =>
        User.RegisterWithExternalProvider(ExternalLoginProvider.Google, Guid.NewGuid().ToString(), email, _time.GetUtcNow());

    private static Mock<IJwtService> Jwt()
    {
        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.CreateAccessToken(It.IsAny<Guid>()))
            .Returns(new AccessToken("access", DateTimeOffset.UtcNow.AddHours(1)));
        jwt.Setup(j => j.GenerateRefreshToken())
            .Returns(new RefreshToken("refresh", DateTimeOffset.UtcNow.AddDays(30)));
        return jwt;
    }

    [Fact]
    public async Task LoginCode_IsSentOnlyToAdmins_AndWorksOnce()
    {
        await using var db = InMemoryDb.Create();
        var admin = ExternalUser("boss@example.com");
        var other = ExternalUser("someone@example.com");
        db.Users.AddRange(admin, other);
        await db.SaveChangesAsync();

        string? sentCode = null;
        var email = new Mock<IEmailSender>();
        email.Setup(e => e.SendAdminLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, code, _) => sentCode = code)
            .ReturnsAsync(EmailSendResult.Ok());

        var admins = new Admins(admin.Id);
        var request = new RequestAdminLoginCodeCommandHandler(
            db, admins, new PlainHasher(), email.Object, _time,
            NullLogger<RequestAdminLoginCodeCommandHandler>.Instance);

        (await request.Handle(new RequestAdminLoginCodeCommand("someone@example.com"), default))
            .IsSuccess.Should().BeTrue("yanıt yönetici olmayanı belli etmemeli");
        sentCode.Should().BeNull();

        (await request.Handle(new RequestAdminLoginCodeCommand(" Boss@Example.com "), default))
            .IsSuccess.Should().BeTrue();
        sentCode.Should().NotBeNull();

        var verify = new VerifyAdminLoginCodeCommandHandler(db, admins, new PlainHasher(), Jwt().Object, _time);

        (await verify.Handle(new VerifyAdminLoginCodeCommand("boss@example.com", "000000x", null, null), default))
            .IsSuccess.Should().BeFalse();

        var ok = await verify.Handle(new VerifyAdminLoginCodeCommand("boss@example.com", sentCode!, null, null), default);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.AccessToken.Should().Be("access");

        (await verify.Handle(new VerifyAdminLoginCodeCommand("boss@example.com", sentCode!, null, null), default))
            .IsSuccess.Should().BeFalse("kod tek kullanımlık");
    }

    [Fact]
    public async Task LoginCode_WorksForGoogleOnlyAccount_LoadedFromDatabase()
    {
        // Regresyon: kullanıcı yeni bir bağlamdan yüklendiğinde dış girişleri boş geliyordu
        // ve şifresiz hesap "giriş yapamaz" sayılıp 400 dönüyordu.
        var name = Guid.NewGuid().ToString("N");
        var admin = ExternalUser("boss@example.com");
        await using (var seed = InMemoryDb.Create(name))
        {
            seed.Users.Add(admin);
            await seed.SaveChangesAsync();
        }

        var admins = new Admins(admin.Id);
        string? sentCode = null;
        var email = new Mock<IEmailSender>();
        email.Setup(e => e.SendAdminLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, code, _) => sentCode = code)
            .ReturnsAsync(EmailSendResult.Ok());

        await using (var db = InMemoryDb.Create(name))
        {
            await new RequestAdminLoginCodeCommandHandler(db, admins, new PlainHasher(), email.Object, _time,
                NullLogger<RequestAdminLoginCodeCommandHandler>.Instance)
                .Handle(new RequestAdminLoginCodeCommand("boss@example.com"), default);
        }

        await using (var db = InMemoryDb.Create(name))
        {
            var result = await new VerifyAdminLoginCodeCommandHandler(db, admins, new PlainHasher(), Jwt().Object, _time)
                .Handle(new VerifyAdminLoginCodeCommand("boss@example.com", sentCode!, null, null), default);
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task LoginCode_Expires()
    {
        await using var db = InMemoryDb.Create();
        var admin = ExternalUser("boss@example.com");
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        string? sentCode = null;
        var email = new Mock<IEmailSender>();
        email.Setup(e => e.SendAdminLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, code, _) => sentCode = code)
            .ReturnsAsync(EmailSendResult.Ok());
        var admins = new Admins(admin.Id);

        await new RequestAdminLoginCodeCommandHandler(db, admins, new PlainHasher(), email.Object, _time,
            NullLogger<RequestAdminLoginCodeCommandHandler>.Instance)
            .Handle(new RequestAdminLoginCodeCommand("boss@example.com"), default);

        _time.Advance(TimeSpan.FromMinutes(11));

        (await new VerifyAdminLoginCodeCommandHandler(db, admins, new PlainHasher(), Jwt().Object, _time)
            .Handle(new VerifyAdminLoginCodeCommand("boss@example.com", sentCode!, null, null), default))
            .IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Ban_RevokesSessions_RemovesAvatar_AndCanBeUndone()
    {
        await using var db = InMemoryDb.Create();
        var admin = ExternalUser("boss@example.com");
        var target = ExternalUser("bad@example.com");
        var profile = UserProfile.Create(target.Id, "kotu", "Kotu", _time.GetUtcNow());
        profile.UpdateAvatar(
            "https://x.supabase.co/storage/v1/object/public/avatars/" + target.Id + "/a.jpg", _time.GetUtcNow());
        target.AttachUserProfile(profile);
        db.Users.AddRange(admin, target);
        db.UserSessions.Add(UserSession.Create(target.Id, "s", _time.GetUtcNow().AddDays(30), _time.GetUtcNow()));
        await db.SaveChangesAsync();

        var storage = new Mock<IFileStorage>();
        var admins = new Admins(admin.Id);
        var ban = new BanUserCommandHandler(db, new TestCurrentUser(admin.Id), admins, storage.Object, _time,
            NullLogger<BanUserCommandHandler>.Instance);

        (await ban.Handle(new BanUserCommand(target.Id, "uygunsuz ad", RemoveAvatar: true), default))
            .IsSuccess.Should().BeTrue();

        var saved = await db.Users.Include(user => user.UserProfile).SingleAsync(user => user.Id == target.Id);
        saved.Status.Should().Be(UserStatus.Banned);
        saved.UserProfile!.ProfileImageUrl.Should().BeNull();
        (await db.UserSessions.SingleAsync()).RevokedAt.Should().NotBeNull();
        storage.Verify(s => s.DeleteAsync("avatars", $"{target.Id}/a.jpg", It.IsAny<CancellationToken>()), Times.Once);
        db.AdminAuditEntries.Should().HaveCount(2);

        (await new UnbanUserCommandHandler(db, new TestCurrentUser(admin.Id), _time)
            .Handle(new UnbanUserCommand(target.Id), default)).IsSuccess.Should().BeTrue();
        (await db.Users.SingleAsync(user => user.Id == target.Id)).Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Ban_RefusesAdminAccounts()
    {
        await using var db = InMemoryDb.Create();
        var admin = ExternalUser("boss@example.com");
        var otherAdmin = ExternalUser("boss2@example.com");
        db.Users.AddRange(admin, otherAdmin);
        await db.SaveChangesAsync();

        var result = await new BanUserCommandHandler(db, new TestCurrentUser(admin.Id), new Admins(admin.Id, otherAdmin.Id),
                Mock.Of<IFileStorage>(), _time, NullLogger<BanUserCommandHandler>.Instance)
            .Handle(new BanUserCommand(otherAdmin.Id, null, false), default);

        result.IsSuccess.Should().BeFalse();
        (await db.Users.SingleAsync(user => user.Id == otherAdmin.Id)).Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Reply_IsSavedOnlyAfterEmailIsSent()
    {
        await using var db = InMemoryDb.Create();
        var admin = ExternalUser("boss@example.com");
        var author = ExternalUser("fan@example.com");
        var feedback = AppFeedback.Create(author.Id, "Harita bazen açılmıyor.", _time.GetUtcNow());
        db.Users.AddRange(admin, author);
        db.AppFeedbacks.Add(feedback);
        await db.SaveChangesAsync();

        var email = new Mock<IEmailSender>();
        email.SetupSequence(e => e.SendFeedbackReplyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendResult.Failed("down"))
            .ReturnsAsync(EmailSendResult.Ok());
        var handler = new ReplyToFeedbackCommandHandler(db, new TestCurrentUser(admin.Id), email.Object, _time,
            NullLogger<ReplyToFeedbackCommandHandler>.Instance);

        (await handler.Handle(new ReplyToFeedbackCommand(feedback.Id, "Düzelttik, teşekkürler!"), default))
            .IsSuccess.Should().BeFalse();
        (await db.AppFeedbacks.SingleAsync()).AdminReply.Should().BeNull();

        (await handler.Handle(new ReplyToFeedbackCommand(feedback.Id, "Düzelttik, teşekkürler!"), default))
            .IsSuccess.Should().BeTrue();
        var saved = await db.AppFeedbacks.SingleAsync();
        saved.AdminReply.Should().Be("Düzelttik, teşekkürler!");
        saved.Status.Should().Be(AppFeedbackStatus.Resolved);
        email.Verify(e => e.SendFeedbackReplyAsync("fan@example.com", "Harita bazen açılmıyor.",
            "Düzelttik, teşekkürler!", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
