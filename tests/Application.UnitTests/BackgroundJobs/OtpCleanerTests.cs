using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Infrastructure.Authentication;

namespace Sportner.Application.UnitTests.BackgroundJobs;

public sealed class OtpCleanerTests
{
    [Fact]
    public async Task Cleanup_RemovesOnlyExpiredChallenges()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var store = new InMemoryOtpChallengeStore(time);

        await store.SetAsync("+905551111111", "hash-old", time.GetUtcNow().AddMinutes(-1));
        await store.SetAsync("+905552222222", "hash-new", time.GetUtcNow().AddMinutes(5));

        var cleaner = new OtpCleaner(store, time, NullLogger<OtpCleaner>.Instance);
        var removed = await cleaner.CleanupAsync();

        removed.Should().Be(1);
        (await store.GetHashAsync("+905551111111")).Should().BeNull();
        (await store.GetHashAsync("+905552222222")).Should().Be("hash-new");
    }
}
