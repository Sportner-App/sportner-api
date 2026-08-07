using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.BackgroundJobs;

public sealed class ExpiredSessionCleanerTests
{
    [Fact]
    public async Task Cleanup_RemovesExpiredSessions_KeepsActive()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var user = TestUsers.CreateActive("+905551111111", time.GetUtcNow());

        var expired = UserSession.Create(
            user.Id,
            "expired-hash",
            time.GetUtcNow().AddDays(1),
            time.GetUtcNow());

        var active = UserSession.Create(
            user.Id,
            "active-hash",
            time.GetUtcNow().AddDays(400),
            time.GetUtcNow());

        db.Users.Add(user);
        db.UserSessions.AddRange(expired, active);
        await db.SaveChangesAsync();

        time.Advance(TimeSpan.FromDays(100));

        var cleaner = new ExpiredSessionCleaner(
            db,
            time,
            Options.Create(new BackgroundJobsOptions { SessionRetentionDays = 90, SessionCleanupBatchSize = 100 }),
            NullLogger<ExpiredSessionCleaner>.Instance);

        var deleted = await cleaner.CleanupAsync();

        deleted.Should().Be(1);
        (await db.UserSessions.FindAsync(expired.Id)).Should().BeNull();
        (await db.UserSessions.FindAsync(active.Id)).Should().NotBeNull();
    }
}
