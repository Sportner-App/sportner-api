using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Users;

public class UserSession : AuditableEntity
{
    private UserSession()
    {
    }

    public Guid UserId { get; private set; }

    public Guid? DeviceId { get; private set; }

    public string RefreshTokenHash { get; private set; } = null!;

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static UserSession Create(
        Guid userId,
        string refreshTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset utcNow,
        Guid? deviceId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (deviceId == Guid.Empty)
        {
            throw new DomainException("Device id cannot be empty.");
        }

        var normalizedHash = NormalizeRefreshTokenHash(refreshTokenHash);

        if (expiresAt <= utcNow)
        {
            throw new DomainException("Session expiration must be later than creation time.");
        }

        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = deviceId,
            RefreshTokenHash = normalizedHash,
            IpAddress = NormalizeOptionalIpAddress(ipAddress),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            ExpiresAt = expiresAt,
            CreatedAt = utcNow
        };
    }

    public void RotateRefreshToken(
        string refreshTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset utcNow)
    {
        if (RevokedAt is not null)
        {
            throw new DomainException("Revoked sessions cannot be rotated.");
        }

        if (IsExpired(utcNow))
        {
            throw new DomainException("Expired sessions cannot be rotated.");
        }

        if (expiresAt <= utcNow)
        {
            throw new DomainException("Session expiration must be later than the current time.");
        }

        RefreshTokenHash = NormalizeRefreshTokenHash(refreshTokenHash);
        ExpiresAt = expiresAt;
        Touch(utcNow);
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = utcNow;
        Touch(utcNow);
    }

    public bool IsActive(DateTimeOffset utcNow)
    {
        return RevokedAt is null && !IsExpired(utcNow);
    }

    public bool IsExpired(DateTimeOffset utcNow)
    {
        return utcNow >= ExpiresAt;
    }

    public bool BelongsToDevice(Guid deviceId)
    {
        return DeviceId == deviceId;
    }

    public override string ToString()
    {
        return $"UserSession:{Id}";
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeRefreshTokenHash(string refreshTokenHash)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            throw new DomainException("Refresh token hash is required.");
        }

        return refreshTokenHash.Trim();
    }

    private static string? NormalizeOptionalIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        var normalized = ipAddress.Trim();

        if (normalized.Length > 45)
        {
            throw new DomainException("IP address cannot exceed 45 characters.");
        }

        return normalized;
    }
}
