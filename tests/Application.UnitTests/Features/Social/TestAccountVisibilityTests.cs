using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Features.Social;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Social;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Social;

public sealed class TestAccountVisibilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HiddenUserIds_RealViewer_HidesTestAccountsAndBlocks()
    {
        await using var db = InMemoryDb.Create();
        var (viewer, tester, blocked, otherTester) = await SeedAsync(db);

        var hidden = await BlockQueries.HiddenUserIds(db, viewer.Id).ToListAsync();

        hidden.Should().BeEquivalentTo([tester.Id, otherTester.Id, blocked.Id]);
    }

    [Fact]
    public async Task HiddenUserIds_Anonymous_HidesOnlyTestAccounts()
    {
        await using var db = InMemoryDb.Create();
        var (_, tester, _, otherTester) = await SeedAsync(db);

        var hidden = await BlockQueries.HiddenUserIds(db, null).ToListAsync();

        hidden.Should().BeEquivalentTo([tester.Id, otherTester.Id]);
    }

    [Fact]
    public async Task HiddenUserIds_TestViewer_SeesEveryoneExceptBlocks()
    {
        await using var db = InMemoryDb.Create();
        var (_, tester, _, otherTester) = await SeedAsync(db);
        var victim = CreateUser(db, "victim", isTest: false);
        db.UserBlocks.Add(UserBlock.Create(tester.Id, victim.Id, Now));
        await db.SaveChangesAsync();

        var hidden = await BlockQueries.HiddenUserIds(db, tester.Id).ToListAsync();

        hidden.Should().BeEquivalentTo([victim.Id]);
        hidden.Should().NotContain(otherTester.Id);
    }

    private static async Task<(User Viewer, User Tester, User Blocked, User OtherTester)> SeedAsync(AppDbContext db)
    {
        var viewer = CreateUser(db, "viewer", isTest: false);
        var tester = CreateUser(db, "tester", isTest: true);
        var otherTester = CreateUser(db, "tester2", isTest: true);
        var blocked = CreateUser(db, "blocked", isTest: false);
        db.UserBlocks.Add(UserBlock.Create(viewer.Id, blocked.Id, Now));
        await db.SaveChangesAsync();
        return (viewer, tester, blocked, otherTester);
    }

    private static User CreateUser(AppDbContext db, string username, bool isTest)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = TestUsers.CreateActive($"+9055{suffix}", Now);
        var profile = UserProfile.Create(user.Id, username, username, Now, isProfilePublic: true);
        user.AttachUserProfile(profile);
        if (isTest)
        {
            user.MarkAsTestAccount();
        }

        db.Users.Add(user);
        db.UserProfiles.Add(profile);
        return user;
    }
}
