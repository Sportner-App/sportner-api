namespace Sportner.Domain.Common.Enums;

public enum ParticipantStatus : short
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Attended = 4,
    NoShow = 5,
    Invited = 6
}
