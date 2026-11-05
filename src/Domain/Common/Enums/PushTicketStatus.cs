namespace Sportner.Domain.Common.Enums;

public enum PushTicketStatus : short
{
    Pending = 0,
    Delivered = 1,
    Failed = 2,

    /// <summary>Expo did not return a receipt within its retention window.</summary>
    Expired = 3
}
