namespace Sportner.Domain.Enums;

public enum ParticipantStatus
{
    Pending,
    Approved,
    Rejected
}

public static class ParticipantStatusExtensions
{
    public static string ToDbValue(this ParticipantStatus status) => status switch
    {
        ParticipantStatus.Pending => "pending",
        ParticipantStatus.Approved => "approved",
        ParticipantStatus.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static ParticipantStatus ParseDbValue(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "pending" => ParticipantStatus.Pending,
            "approved" => ParticipantStatus.Approved,
            "rejected" => ParticipantStatus.Rejected,
            _ => throw new ArgumentException($"Unknown participant status: '{value}'", nameof(value))
        };

    public static bool TryParseDbValue(string? value, out ParticipantStatus status)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "pending":
                status = ParticipantStatus.Pending;
                return true;
            case "approved":
                status = ParticipantStatus.Approved;
                return true;
            case "rejected":
                status = ParticipantStatus.Rejected;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
