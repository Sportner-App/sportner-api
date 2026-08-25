namespace Sportner.Application.BackgroundJobs;

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When true, each job runs once immediately after the worker starts (handy for local verify).
    /// </summary>
    public bool RunOnStartup { get; set; }

    public int SessionRetentionDays { get; set; } = 90;

    public int SessionCleanupBatchSize { get; set; } = 500;

    /// <summary>Daily 03:00 UTC.</summary>
    public string SessionCleanupCron { get; set; } = "0 3 * * *";

    /// <summary>Every 5 minutes — close events past eventDate + duration.</summary>
    public string EventCompletionCron { get; set; } = "*/5 * * * *";

    public int EventCompletionBatchSize { get; set; } = 50;

    /// <summary>Every 15 minutes.</summary>
    public string EventReminderCron { get; set; } = "*/15 * * * *";

    /// <summary>Reminder windows in minutes before event start (defaults: 24h + 1h).</summary>
    public int[] EventReminderWindowsMinutes { get; set; } = [1440, 60];

    /// <summary>Every minute.</summary>
    public string NotificationDeliveryCron { get; set; } = "* * * * *";

    public int NotificationDeliveryBatchSize { get; set; } = 100;

    /// <summary>Daily 04:00 UTC — MARATHON_RUNNER streak sweep.</summary>
    public string MarathonRunnerBadgeCron { get; set; } = "0 4 * * *";
}
