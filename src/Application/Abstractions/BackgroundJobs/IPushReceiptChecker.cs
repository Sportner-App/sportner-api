namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IPushReceiptChecker
{
    Task CheckDueAsync(CancellationToken cancellationToken = default);
}
