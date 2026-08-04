namespace Sportner.Domain.Common.Enums;

public enum NotificationType : short
{
    FriendRequest = 0,
    FriendAccepted = 1,
    EventInvitation = 2,
    EventRequestApproved = 3,
    EventRequestRejected = 4,
    EventReminder = 5,
    EventCancelled = 6,
    PostLiked = 7,
    PostCommented = 8,
    CommentReplied = 9,
    BadgeEarned = 10,
    NewMessage = 11,
    System = 12
}
