using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Users;

public class UserDevice : AuditableEntity
{
    private UserDevice()
    {
    }

    public Guid UserId { get; private set; }

    public DevicePlatform Platform { get; private set; }

    public string? DeviceName { get; private set; }

    public string DeviceIdentifier { get; private set; } = null!;

    public string? AppVersion { get; private set; }

    public string? OsVersion { get; private set; }

    public string? PushToken { get; private set; }

    public DateTimeOffset? LastSeenAt { get; private set; }

    public static UserDevice Create(
        Guid userId,
        DevicePlatform platform,
        string deviceIdentifier,
        DateTimeOffset utcNow,
        string? deviceName = null,
        string? appVersion = null,
        string? osVersion = null,
        string? pushToken = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        EnsureDefinedPlatform(platform);

        return new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = platform,
            DeviceIdentifier = NormalizeDeviceIdentifier(deviceIdentifier),
            DeviceName = NormalizeOptionalText(deviceName, 100, "Device name"),
            AppVersion = NormalizeOptionalText(appVersion, 30, "App version"),
            OsVersion = NormalizeOptionalText(osVersion, 30, "OS version"),
            PushToken = NormalizeOptionalPushToken(pushToken),
            LastSeenAt = utcNow,
            CreatedAt = utcNow
        };
    }

    public void UpdatePushToken(string? pushToken, DateTimeOffset utcNow)
    {
        PushToken = NormalizeOptionalPushToken(pushToken);
        Touch(utcNow);
    }

    public void UpdateDeviceInformation(
        DateTimeOffset utcNow,
        string? deviceName = null,
        string? appVersion = null,
        string? osVersion = null)
    {
        DeviceName = NormalizeOptionalText(deviceName, 100, "Device name");
        AppVersion = NormalizeOptionalText(appVersion, 30, "App version");
        OsVersion = NormalizeOptionalText(osVersion, 30, "OS version");
        Touch(utcNow);
    }

    public void RecordActivity(DateTimeOffset utcNow)
    {
        if (LastSeenAt is not null && utcNow < LastSeenAt.Value)
        {
            throw new DomainException("Last seen timestamp cannot move backwards.");
        }

        LastSeenAt = utcNow;
        Touch(utcNow);
    }

    public void ClearPushToken(DateTimeOffset utcNow)
    {
        PushToken = null;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static void EnsureDefinedPlatform(DevicePlatform platform)
    {
        if (!Enum.IsDefined(platform))
        {
            throw new DomainException("Device platform is invalid.");
        }
    }

    private static string NormalizeDeviceIdentifier(string deviceIdentifier)
    {
        if (string.IsNullOrWhiteSpace(deviceIdentifier))
        {
            throw new DomainException("Device identifier is required.");
        }

        var normalized = deviceIdentifier.Trim();

        if (normalized.Length > 255)
        {
            throw new DomainException("Device identifier cannot exceed 255 characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalPushToken(string? pushToken)
    {
        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return null;
        }

        return pushToken.Trim();
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
