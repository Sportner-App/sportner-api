namespace Sportner.Domain.Common.Enums;

public enum NotificationDeliveryStatus : short
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Cancelled = 3,

    /// <summary>
    /// Claimed by a dispatcher instance and currently being sent. Prevents another
    /// concurrently-running dispatcher (e.g. the API's inline delivery service and the
    /// dedicated Notifications worker) from picking up the same row and double-sending.
    /// </summary>
    Processing = 4
}
