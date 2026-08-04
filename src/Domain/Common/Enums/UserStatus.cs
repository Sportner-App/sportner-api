namespace Sportner.Domain.Common.Enums;

public enum UserStatus : short
{
    PendingVerification = 0,
    Active = 1,
    Suspended = 2,
    Banned = 3,
    Deleted = 4
}
