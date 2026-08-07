using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Infrastructure.Authentication;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class OtpServiceTests
{
    [Fact]
    public async Task Request_UsesFixedCode_WhenExposeEnabled()
    {
        var store = new InMemoryOtpChallengeStore(
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        var hasher = new TokenHasher();
        var sms = new Mock<ISmsSender>();
        sms.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new OtpService(
            store,
            hasher,
            sms.Object,
            Options.Create(new OtpOptions
            {
                CodeLength = 6,
                ExpirationMinutes = 5,
                ExposeCodeInLogs = true,
                FixedCode = "000000"
            }),
            TimeProvider.System,
            NullLogger<OtpService>.Instance);

        await service.RequestAsync("+905551112233");

        (await service.VerifyAsync("+905551112233", "000000")).Should().BeTrue();
    }

    [Fact]
    public async Task Request_IgnoresFixedCode_WhenExposeDisabled()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var store = new InMemoryOtpChallengeStore(time);
        var hasher = new TokenHasher();
        var sms = new Mock<ISmsSender>();
        sms.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new OtpService(
            store,
            hasher,
            sms.Object,
            Options.Create(new OtpOptions
            {
                CodeLength = 6,
                ExpirationMinutes = 5,
                ExposeCodeInLogs = false,
                FixedCode = "000000"
            }),
            time,
            NullLogger<OtpService>.Instance);

        await service.RequestAsync("+905551112233");

        (await service.VerifyAsync("+905551112233", "000000")).Should().BeFalse();
    }
}
