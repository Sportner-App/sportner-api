using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.BackgroundJobs;

public sealed class PushReceiptCheckerTests
{
    private const string Token = "ExponentPushToken[old]";

    private static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CheckDueAsync_DeadToken_FailsTicketAndClearsToken()
    {
        await using var db = InMemoryDb.Create();
        var (device, ticket) = Seed(db, Now.AddMinutes(-20));
        await db.SaveChangesAsync();

        await Checker(db, new PushReceipt(false, true, "DeviceNotRegistered", "gone")).CheckDueAsync();

        ticket.Status.Should().Be(PushTicketStatus.Failed);
        ticket.ErrorCode.Should().Be("DeviceNotRegistered");
        device.PushToken.Should().BeNull();
    }

    [Fact]
    public async Task CheckDueAsync_DeadToken_KeepsTokenRegisteredAfterTheSend()
    {
        await using var db = InMemoryDb.Create();
        var (device, _) = Seed(db, Now.AddMinutes(-20), ticketToken: "ExponentPushToken[older]");
        await db.SaveChangesAsync();

        await Checker(db, new PushReceipt(false, true, "DeviceNotRegistered", "gone")).CheckDueAsync();

        device.PushToken.Should().Be(Token);
    }

    [Fact]
    public async Task CheckDueAsync_OkReceipt_MarksDelivered()
    {
        await using var db = InMemoryDb.Create();
        var (device, ticket) = Seed(db, Now.AddMinutes(-20));
        await db.SaveChangesAsync();

        await Checker(db, new PushReceipt(true, false, null, null)).CheckDueAsync();

        ticket.Status.Should().Be(PushTicketStatus.Delivered);
        device.PushToken.Should().Be(Token);
    }

    [Fact]
    public async Task CheckDueAsync_NoReceipt_ReschedulesThenExpires()
    {
        await using var db = InMemoryDb.Create();
        var (_, recent) = Seed(db, Now.AddMinutes(-20));
        var (_, old) = Seed(db, Now.AddHours(-25));
        await db.SaveChangesAsync();

        await Checker(db, null).CheckDueAsync();

        recent.Status.Should().Be(PushTicketStatus.Pending);
        recent.CheckAfter.Should().BeAfter(Now);
        old.Status.Should().Be(PushTicketStatus.Expired);
    }

    private static PushReceiptChecker Checker(Sportner.Infrastructure.Persistence.AppDbContext db, PushReceipt? receipt)
    {
        var sender = new Mock<IPushSender>();
        sender
            .Setup(candidate => candidate.GetReceiptsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<string> ids, CancellationToken _) =>
                receipt is null
                    ? new Dictionary<string, PushReceipt>()
                    : ids.ToDictionary(id => id, _ => receipt));

        return new PushReceiptChecker(
            db,
            sender.Object,
            new FakeTimeProvider(Now),
            Options.Create(new BackgroundJobsOptions()),
            NullLogger<PushReceiptChecker>.Instance);
    }

    private static (UserDevice Device, PushDeliveryTicket Ticket) Seed(
        Sportner.Infrastructure.Persistence.AppDbContext db,
        DateTimeOffset createdAt,
        string ticketToken = Token)
    {
        var device = UserDevice.Create(
            Guid.NewGuid(),
            DevicePlatform.iOS,
            Guid.NewGuid().ToString("N"),
            createdAt,
            pushToken: Token);
        var ticket = PushDeliveryTicket.Create(
            Guid.NewGuid(),
            device.Id,
            Guid.NewGuid().ToString("N"),
            ticketToken,
            createdAt.AddMinutes(15),
            createdAt);

        db.UserDevices.Add(device);
        db.PushDeliveryTickets.Add(ticket);
        return (device, ticket);
    }
}
