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
    private readonly List<UserExternalLogin> _externalLogins = [];

    private User()
    {
    }

    public string? PhoneNumber { get; private set; }

    public DateTimeOffset? PhoneVerifiedAt { get; private set; }

    /// <summary>ASP.NET Identity compatible password hash. Null = cannot sign in with password.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Normalized (trimmed, lowercased) email. Unique across all accounts.</summary>
    public string? Email { get; private set; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    /// <summary>Hash of the currently outstanding verification code; null once verified or never issued.</summary>
    public string? EmailVerificationCodeHash { get; private set; }

    public DateTimeOffset? EmailVerificationCodeExpiresAt { get; private set; }

    /// <summary>When the last verification code was sent — drives the resend cooldown.</summary>
    public DateTimeOffset? EmailVerificationSentAt { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTimeOffset? LastSeenAt { get; private set; }

    public DateTimeOffset? OnboardingCompletedAt { get; private set; }

    public UserProfile? UserProfile { get; private set; }

    public UserStatistics? Statistics { get; private set; }

    public IReadOnlyCollection<UserSport> Sports => _sports.AsReadOnly();

    public IReadOnlyCollection<UserSavedLocation> SavedLocations => _savedLocations.AsReadOnly();

    public IReadOnlyCollection<UserDevice> Devices => _devices.AsReadOnly();

    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();

    public IReadOnlyCollection<UserExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

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

    /// <summary>V1 password auth: active account with password hash; phone optional. Email is
    /// required and starts unverified — a verification code is issued separately.</summary>
    public static User RegisterWithPassword(string passwordHash, string email, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            CreatedAt = utcNow
        };

        user.Statistics = UserStatistics.Create(user.Id, utcNow);
        user.SetEmail(email, isVerified: false, utcNow);

        return user;
    }

    /// <summary>
    /// Social sign-in: the provider's identity assertion stands in for phone/password verification.
    /// When the provider supplies an email it's trusted as already verified — Google/Apple only
    /// hand back an identity token for an account they've already confirmed ownership of.
    /// </summary>
    public static User RegisterWithExternalProvider(
        ExternalLoginProvider provider,
        string providerUserId,
        string? email,
        DateTimeOffset utcNow)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Status = UserStatus.Active,
            CreatedAt = utcNow
        };

        user.Statistics = UserStatistics.Create(user.Id, utcNow);
        user._externalLogins.Add(
            UserExternalLogin.Create(user.Id, provider, providerUserId, email, utcNow));

        if (!string.IsNullOrWhiteSpace(email))
        {
            user.SetEmail(email, isVerified: true, utcNow);
        }

        return user;
    }

    public void SetPasswordHash(string passwordHash, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
        Touch(utcNow);
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

    /// <summary>
    /// Self-service account deletion. Soft delete only (Status field, per project
    /// convention) — no row or related data is physically removed. Revokes every
    /// active session so any token issued before deletion stops working immediately,
    /// since <see cref="EnsureNotDeleted"/> would otherwise block the usual
    /// <see cref="RevokeAllSessions"/> call once the status flips.
    /// </summary>
    public void Delete(DateTimeOffset utcNow)
    {
        if (Status is UserStatus.Deleted)
        {
            throw new DomainException("User is already deleted.");
        }

        foreach (var session in _sessions.Where(session => session.IsActive(utcNow)))
        {
            session.Revoke(utcNow);
        }

        Status = UserStatus.Deleted;
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

    /// <summary>
    /// Sets the account email. Used once at registration — there is no self-service change
    /// flow yet, matching how birth date is locked after first set (both are identity-adjacent
    /// fields where uncontrolled edits would undermine the checks built on top of them).
    /// </summary>
    private void SetEmail(string email, bool isVerified, DateTimeOffset utcNow)
    {
        Email = NormalizeEmail(email);
        EmailVerifiedAt = isVerified ? utcNow : null;
        EmailVerificationCodeHash = null;
        EmailVerificationCodeExpiresAt = null;
        EmailVerificationSentAt = null;
    }

    /// <summary>
    /// Records a freshly-sent verification code (already hashed by the caller — the domain
    /// never sees the raw code). <paramref name="utcNow"/> also stamps the resend cooldown.
    /// </summary>
    public void IssueEmailVerificationCode(string codeHash, DateTimeOffset expiresAt, DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(Email))
        {
            throw new DomainException("Cannot issue a verification code without an email.");
        }

        if (EmailVerifiedAt is not null)
        {
            throw new DomainException("Email is already verified.");
        }

        EmailVerificationCodeHash = codeHash;
        EmailVerificationCodeExpiresAt = expiresAt;
        EmailVerificationSentAt = utcNow;
        Touch(utcNow);
    }

    /// <summary>True once verified, or while a resend would arrive before the cooldown elapses.</summary>
    public bool CanResendEmailVerificationCode(DateTimeOffset utcNow, TimeSpan cooldown) =>
        EmailVerifiedAt is null
        && (EmailVerificationSentAt is null || utcNow - EmailVerificationSentAt >= cooldown);

    /// <summary>
    /// Marks the email verified. The caller is responsible for checking the submitted code
    /// against <see cref="EmailVerificationCodeHash"/> and its expiry before calling this —
    /// hashing/comparison lives in the application layer where <c>ITokenHasher</c> is injected.
    /// </summary>
    public void ConfirmEmailVerified(DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        EmailVerifiedAt = utcNow;
        EmailVerificationCodeHash = null;
        EmailVerificationCodeExpiresAt = null;
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

    public void CompleteOnboarding(DateTimeOffset utcNow)
    {
        EnsureNotDeleted();

        if (OnboardingCompletedAt is not null)
        {
            return;
        }

        if (UserProfile is null)
        {
            throw new DomainException("Onboarding cannot be completed before the profile is created.");
        }

        if (_sports.Count == 0)
        {
            throw new DomainException("Onboarding cannot be completed before a sport is selected.");
        }

        OnboardingCompletedAt = utcNow;
        Touch(utcNow);
    }

    public void AttachUserProfile(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (UserProfile is not null)
        {
            throw new DomainException("User already has a profile.");
        }

        if (profile.UserId != Id)
        {
            throw new DomainException("Profile does not belong to this user.");
        }

        UserProfile = profile;
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
        return Status is UserStatus.Active
            && (!string.IsNullOrWhiteSpace(PasswordHash) || _externalLogins.Count > 0);
    }

    public bool HasCompletedOnboarding()
    {
        return OnboardingCompletedAt is not null;
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

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > 254)
        {
            throw new DomainException("Email cannot exceed 254 characters.");
        }

        return normalized;
    }
}
