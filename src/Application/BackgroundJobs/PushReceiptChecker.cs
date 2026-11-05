using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.BackgroundJobs;

/// <summary>
/// Reads Expo receipts for accepted pushes. The API and the Notifications worker may both run
/// this; resolving a ticket twice gives the same outcome, so no claim step is needed.
/// </summary>
internal sealed class PushReceiptChecker : IPushReceiptChecker
{
    /// <summary>Expo drops receipts after about a day.</summary>
    private static readonly TimeSpan ReceiptRetention = TimeSpan.FromHours(24);

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _dbContext;
    private readonly IPushSender _pushSender;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<PushReceiptChecker> _logger;

    public PushReceiptChecker(
        IApplicationDbContext dbContext,
        IPushSender pushSender,
        TimeProvider timeProvider,
        IOptions<BackgroundJobsOptions> options,
        ILogger<PushReceiptChecker> logger)
    {
        _dbContext = dbContext;
        _pushSender = pushSender;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task CheckDueAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var batchSize = Math.Max(1, _options.PushReceiptBatchSize);

        var tickets = await _dbContext.PushDeliveryTickets
            .Where(ticket => ticket.Status == PushTicketStatus.Pending && ticket.CheckAfter <= utcNow)
            .OrderBy(ticket => ticket.CheckAfter)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (tickets.Count == 0)
        {
            return;
        }

        var receipts = await _pushSender.GetReceiptsAsync(
            tickets.Select(ticket => ticket.TicketId).ToList(),
            cancellationToken);

        foreach (var ticket in tickets)
        {
            if (!receipts.TryGetValue(ticket.TicketId, out var receipt))
            {
                if (utcNow - ticket.CreatedAt >= ReceiptRetention)
                {
                    ticket.MarkExpired(utcNow);
                }
                else
                {
                    ticket.Reschedule(utcNow + RecheckDelay, utcNow);
                }

                continue;
            }

            if (receipt.Delivered)
            {
                ticket.MarkDelivered(utcNow);
                continue;
            }

            ticket.MarkFailed(receipt.ErrorCode, receipt.ErrorMessage, utcNow);

            _logger.LogWarning(
                "Push ticket {TicketId} for device {DeviceId} failed: {ErrorCode} {ErrorMessage}",
                ticket.TicketId,
                ticket.UserDeviceId,
                receipt.ErrorCode,
                receipt.ErrorMessage);

            if (receipt.InvalidToken)
            {
                var device = await _dbContext.UserDevices.FirstOrDefaultAsync(
                    candidate => candidate.Id == ticket.UserDeviceId,
                    cancellationToken);

                // The device may have registered a new token since this send.
                if (device is not null && device.PushToken == ticket.PushToken)
                {
                    device.ClearPushToken(utcNow);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
