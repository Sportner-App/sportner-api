using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Users;

public class User : AggregateRoot
{
    private readonly List<UserSport> _sports = [];
    private readonly List<UserSavedLocation> _savedLocations = [];
    private readonly List<UserDevice> _devices = [];
    private readonly List<UserSession> _sessions = [];

    private User()
    {
    }

    public string PhoneNumber { get; private set; } = null!;

    public DateTimeOffset? PhoneVerifiedAt { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTimeOffset? LastSeenAt { get; private set; }

    public Profile? Profile { get; private set; }

    public UserStatistics? Statistics { get; private set; }

    public IReadOnlyCollection<UserSport> Sports => _sports.AsReadOnly();

    public IReadOnlyCollection<UserSavedLocation> SavedLocations => _savedLocations.AsReadOnly();

    public IReadOnlyCollection<UserDevice> Devices => _devices.AsReadOnly();

    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();

    public static User Create(string phoneNumber, DateTimeOffset utcNow)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        var user = new User
        {
            Id = Guid.NewGuid(),
            PhoneNumber = normalizedPhone,
            Status = UserStatus.PendingVerification,
            CreatedAt = utcNow
        };

        user.Statistics = UserStatistics.Create(user.Id, utcNow);

        return user;
    }

    public void Activate(DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        if (PhoneVerifiedAt is null)
        {
            throw new DomainException("Phone number must be verified before activation.");
        }

        if (Status is not (UserStatus.PendingVerification or UserStatus.Suspended))
        {
            throw new DomainException($"User cannot be activated from status '{Status}'.");
        }

        Status = UserStatus.Active;
        Touch(utcNow);
    }

    public void Suspend(DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        if (Status is not UserStatus.Active)
        {
            throw new DomainException($"Only active users can be suspended. Current status: '{Status}'.");
        }

        Status = UserStatus.Suspended;
        Touch(utcNow);
    }

    public void Ban(DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        if (Status is UserStatus.Banned)
        {
            throw new DomainException("User is already banned.");
        }

        if (Status is UserStatus.PendingVerification)
        {
            throw new DomainException("Pending users cannot be banned.");
        }

        Status = UserStatus.Banned;
        Touch(utcNow);
    }

    public void VerifyPhoneNumber(DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        if (Status is UserStatus.Banned or UserStatus.Suspended)
        {
            throw new DomainException($"Phone number cannot be verified while user is '{Status}'.");
        }

        if (PhoneVerifiedAt is not null)
        {
            throw new DomainException("Phone number is already verified.");
        }

        PhoneVerifiedAt = utcNow;
        Touch(utcNow);
    }

    public void UpdateLastLogin(DateTimeOffset utcNow)
    {
        if (!CanAuthenticate())
        {
            throw new DomainException("User cannot update last login in the current state.");
        }

        LastSeenAt = utcNow;
        Touch(utcNow);
    }

    public void AttachProfile(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (Profile is not null)
        {
            throw new DomainException("User already has a profile.");
        }

        if (profile.UserId != Id)
        {
            throw new DomainException("Profile does not belong to this user.");
        }

        Profile = profile;
    }

    public void AttachStatistics(UserStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        if (Statistics is not null)
        {
            throw new DomainException("User already has statistics.");
        }

        if (statistics.UserId != Id)
        {
            throw new DomainException("Statistics do not belong to this user.");
        }

        Statistics = statistics;
    }

    public UserSport AddSport(
        Guid sportId,
        SkillLevel skillLevel,
        DateTimeOffset utcNow,
        bool isPrimary = false)
    {
        EnsureNotDeleted();

        if (_sports.Any(sport => sport.SportId == sportId))
        {
            throw new DomainException("Sport is already associated with this user.");
        }

        var userSport = UserSport.Create(Id, sportId, skillLevel, utcNow, isPrimary: false);

        if (isPrimary)
        {
            ClearPrimarySports(utcNow);
            userSport.MarkAsPrimary(utcNow);
        }

        _sports.Add(userSport);
        Touch(utcNow);

        return userSport;
    }

    public void ChangeSportSkillLevel(Guid sportId, SkillLevel skillLevel, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        var userSport = FindSport(sportId);
        userSport.ChangeSkillLevel(skillLevel, utcNow);
        Touch(utcNow);
    }

    public void SetPrimarySport(Guid sportId, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        var userSport = FindSport(sportId);

        ClearPrimarySports(utcNow);
        userSport.MarkAsPrimary(utcNow);
        Touch(utcNow);
    }

    public void RemoveSport(Guid sportId, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        var userSport = FindSport(sportId);
        _sports.Remove(userSport);
        Touch(utcNow);
    }

    public UserSavedLocation AddSavedLocation(
        string title,
        decimal latitude,
        decimal longitude,
        string address,
        DateTimeOffset utcNow,
        string? city = null,
        string? district = null,
        bool isDefault = false)
    {
        EnsureNotDeleted();

        var location = UserSavedLocation.Create(
            Id,
            title,
            latitude,
            longitude,
            address,
            utcNow,
            city,
            district,
            isDefault: false);

        if (isDefault)
        {
            ClearDefaultLocations(utcNow);
            location.MarkAsDefault(utcNow);
        }

        _savedLocations.Add(location);
        Touch(utcNow);

        return location;
    }

    public void SetDefaultSavedLocation(Guid locationId, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        var location = FindSavedLocation(locationId);

        ClearDefaultLocations(utcNow);
        location.MarkAsDefault(utcNow);
        Touch(utcNow);
    }

    public void RemoveSavedLocation(Guid locationId, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        var location = FindSavedLocation(locationId);
        _savedLocations.Remove(location);
        Touch(utcNow);
    }

    public UserDevice RegisterDevice(
        DevicePlatform platform,
        string deviceIdentifier,
        DateTimeOffset utcNow,
        string? deviceName = null,
        string? appVersion = null,
        string? osVersion = null,
        string? pushToken = null)
    {
        EnsureNotDeleted();

        var normalizedIdentifier = deviceIdentifier.Trim();
        var existing = _devices.FirstOrDefault(device =>
            string.Equals(device.DeviceIdentifier, normalizedIdentifier, StringComparison.Ordinal));

        if (existing is not null)
        {
            if (existing.UserId != Id)
            {
                throw new DomainException("Device does not belong to this user.");
            }

            existing.UpdateDeviceInformation(utcNow, deviceName, appVersion, osVersion);

            if (pushToken is not null)
            {
                existing.UpdatePushToken(pushToken, utcNow);
            }

            existing.RecordActivity(utcNow);
            Touch(utcNow);

            return existing;
        }

        var device = UserDevice.Create(
            Id,
            platform,
            deviceIdentifier,
            utcNow,
            deviceName,
            appVersion,
            osVersion,
            pushToken);

        _devices.Add(device);
        Touch(utcNow);

        return device;
    }

    public void RemoveDevice(Guid deviceId, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        var device = FindDevice(deviceId);

        foreach (var session in _sessions.Where(session =>
                     session.BelongsToDevice(deviceId) && session.IsActive(utcNow)).ToList())
        {
            session.Revoke(utcNow);
        }

        device.ClearPushToken(utcNow);
        _devices.Remove(device);
        Touch(utcNow);
    }

    public UserSession CreateSession(
        string refreshTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset utcNow,
        Guid? deviceId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        EnsureNotDeleted();

        if (!CanAuthenticate())
        {
            throw new DomainException("User cannot create a session in the current state.");
        }

        if (deviceId is not null)
        {
            _ = FindDevice(deviceId.Value);
        }

        var session = UserSession.Create(
            Id,
            refreshTokenHash,
            expiresAt,
            utcNow,
            deviceId,
            ipAddress,
            userAgent);

        _sessions.Add(session);
        Touch(utcNow);

        return session;
    }

    public void RevokeSession(Guid sessionId, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        var session = FindSession(sessionId);
        session.Revoke(utcNow);
        Touch(utcNow);
    }

    public void RevokeAllSessions(DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        foreach (var session in _sessions.Where(session => session.IsActive(utcNow)))
        {
            session.Revoke(utcNow);
        }

        Touch(utcNow);
    }

    public bool CanAuthenticate()
    {
        return PhoneVerifiedAt is not null
            && Status is UserStatus.Active;
    }

    public bool CanCreateContent()
    {
        return CanAuthenticate()
            && Status is not UserStatus.Suspended
            && Status is not UserStatus.Banned;
    }

    private UserSport FindSport(Guid sportId)
    {
        var userSport = _sports.FirstOrDefault(sport => sport.SportId == sportId);

        if (userSport is null)
        {
            throw new DomainException("Sport is not associated with this user.");
        }

        if (userSport.UserId != Id)
        {
            throw new DomainException("Sport does not belong to this user.");
        }

        return userSport;
    }

    private UserSavedLocation FindSavedLocation(Guid locationId)
    {
        var location = _savedLocations.FirstOrDefault(item => item.Id == locationId);

        if (location is null)
        {
            throw new DomainException("Saved location was not found.");
        }

        if (location.UserId != Id)
        {
            throw new DomainException("Saved location does not belong to this user.");
        }

        return location;
    }

    private UserDevice FindDevice(Guid deviceId)
    {
        var device = _devices.FirstOrDefault(item => item.Id == deviceId);

        if (device is null)
        {
            throw new DomainException("Device was not found.");
        }

        if (device.UserId != Id)
        {
            throw new DomainException("Device does not belong to this user.");
        }

        return device;
    }

    private UserSession FindSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(item => item.Id == sessionId);

        if (session is null)
        {
            throw new DomainException("Session was not found.");
        }

        if (session.UserId != Id)
        {
            throw new DomainException("Session does not belong to this user.");
        }

        return session;
    }

    private void ClearPrimarySports(DateTimeOffset utcNow)
    {
        foreach (var sport in _sports.Where(sport => sport.IsPrimary))
        {
            sport.RemovePrimaryStatus(utcNow);
        }
    }

    private void ClearDefaultLocations(DateTimeOffset utcNow)
    {
        foreach (var location in _savedLocations.Where(location => location.IsDefault))
        {
            location.RemoveDefaultStatus(utcNow);
        }
    }

    private void EnsureNotDeleted()
    {
        if (Status is UserStatus.Deleted)
        {
            throw new DomainException("Deleted users cannot be modified.");
        }
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Phone number is required.");
        }

        var normalized = phoneNumber.Trim();

        if (normalized.Length > 20)
        {
            throw new DomainException("Phone number cannot exceed 20 characters.");
        }

        return normalized;
    }
}
