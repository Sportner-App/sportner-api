namespace Sportner.Domain.Enums;

public enum UserEventStatus
{
    Pending,
    Approved,
    Rejected
}

public static class UserEventStatusExtensions
{
    public static string ToDbValue(this UserEventStatus status) => status switch
    {
        UserEventStatus.Pending => "pending",
        UserEventStatus.Approved => "approved",
        UserEventStatus.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static UserEventStatus ParseDbValue(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "pending" => UserEventStatus.Pending,
            "approved" => UserEventStatus.Approved,
            "rejected" => UserEventStatus.Rejected,
            _ => throw new ArgumentException($"Unknown user event status: '{value}'", nameof(value))
        };

    public static bool TryParseDbValue(string? value, out UserEventStatus status)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "pending":
                status = UserEventStatus.Pending;
                return true;
            case "approved":
                status = UserEventStatus.Approved;
                return true;
            case "rejected":
                status = UserEventStatus.Rejected;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
