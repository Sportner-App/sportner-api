namespace Sportner.Domain.Common.Enums;

public enum FriendshipStatus : short
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    /// <summary>
    /// Legacy value. Blocks live on <c>UserBlocks</c>; do not write this status.
    /// </summary>
    Blocked = 3
}
