using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Users;

public class UserProfile : AuditableEntity
{
    private UserProfile()
    {
    }

    public Guid UserId { get; private set; }

    public DateTimeOffset UsernameChangedAt { get; private set; }

    public string Username { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string? LastName { get; private set; }

    public string? Bio { get; private set; }

    public short? Gender { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public string? City { get; private set; }

    public string? ProfileImageUrl { get; private set; }

    public string? IntroVideoUrl { get; private set; }

    public decimal AverageRating { get; private set; }

    public int ReviewCount { get; private set; }

    public bool IsProfilePublic { get; private set; }

    public static UserProfile Create(
        Guid userId,
        string username,
        string firstName,
        DateTimeOffset utcNow,
        string? lastName = null,
        bool isProfilePublic = true)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = NormalizeUsername(username),
            FirstName = NormalizeFirstName(firstName),
            LastName = NormalizeOptionalName(lastName, 50, "Last name"),
            AverageRating = 0m,
            ReviewCount = 0,
            IsProfilePublic = isProfilePublic,
            CreatedAt = utcNow,
            UsernameChangedAt = utcNow
        };

        return profile;
    }

    public void UpdateUsername(string username, DateTimeOffset utcNow)
    {
        var normalized = NormalizeUsername(username);

        if (string.Equals(Username, normalized, StringComparison.Ordinal))
        {
            return;
        }

        if (utcNow - UsernameChangedAt < TimeSpan.FromDays(30))
        {
            throw new DomainException("Username cannot be changed more than once every 30 days.");
        }

        Username = normalized;
        UsernameChangedAt = utcNow;
        Touch(utcNow);
    }

    public void UpdateDisplayName(string firstName, string? lastName, DateTimeOffset utcNow)
    {
        FirstName = NormalizeFirstName(firstName);
        LastName = NormalizeOptionalName(lastName, 50, "Last name");
        Touch(utcNow);
    }

    public void UpdateBio(string? bio, DateTimeOffset utcNow)
    {
        if (bio is not null && bio.Length > 500)
        {
            throw new DomainException("Bio cannot exceed 500 characters.");
        }

        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        Touch(utcNow);
    }

    public void UpdateAvatar(string? profileImageUrl, DateTimeOffset utcNow)
    {
        ProfileImageUrl = NormalizeOptionalUrl(profileImageUrl);
        Touch(utcNow);
    }

    public void UpdateIntroVideo(string? introVideoUrl, DateTimeOffset utcNow)
    {
        IntroVideoUrl = NormalizeOptionalUrl(introVideoUrl);
        Touch(utcNow);
    }

    public void UpdateLocation(string? city, DateTimeOffset utcNow)
    {
        if (city is not null && city.Trim().Length > 100)
        {
            throw new DomainException("City cannot exceed 100 characters.");
        }

        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        Touch(utcNow);
    }

    public void UpdatePersonalDetails(short? gender, DateOnly? birthDate, DateTimeOffset utcNow)
    {
        Gender = gender;
        BirthDate = birthDate;
        Touch(utcNow);
    }

    public void UpdateVisibility(bool isProfilePublic, DateTimeOffset utcNow)
    {
        IsProfilePublic = isProfilePublic;
        Touch(utcNow);
    }

    public void UpdateCachedRating(decimal averageRating, int reviewCount, DateTimeOffset utcNow)
    {
        if (averageRating is < 0m or > 5m)
        {
            throw new DomainException("Average rating must be between 0 and 5.");
        }

        if (reviewCount < 0)
        {
            throw new DomainException("Review count cannot be negative.");
        }

        AverageRating = decimal.Round(averageRating, 2, MidpointRounding.AwayFromZero);
        ReviewCount = reviewCount;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username is required.");
        }

        var normalized = username.Trim();

        if (normalized.Length > 30)
        {
            throw new DomainException("Username cannot exceed 30 characters.");
        }

        return normalized;
    }

    private static string NormalizeFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name is required.");
        }

        var normalized = firstName.Trim();

        if (normalized.Length > 50)
        {
            throw new DomainException("First name cannot exceed 50 characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalName(string? value, int maxLength, string fieldName)
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

    private static string? NormalizeOptionalUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
