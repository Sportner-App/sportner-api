using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Users;

public class UserSavedLocation : AuditableEntity
{
    private UserSavedLocation()
    {
    }

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = null!;

    public decimal Latitude { get; private set; }

    public decimal Longitude { get; private set; }

    public string Address { get; private set; } = null!;

    public string? City { get; private set; }

    public string? District { get; private set; }

    public bool IsDefault { get; private set; }

    public static UserSavedLocation Create(
        Guid userId,
        string title,
        decimal latitude,
        decimal longitude,
        string address,
        DateTimeOffset utcNow,
        string? city = null,
        string? district = null,
        bool isDefault = false)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        return new UserSavedLocation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = NormalizeTitle(title),
            Latitude = NormalizeLatitude(latitude),
            Longitude = NormalizeLongitude(longitude),
            Address = NormalizeAddress(address),
            City = NormalizeOptionalText(city, 100, "City"),
            District = NormalizeOptionalText(district, 100, "District"),
            IsDefault = isDefault,
            CreatedAt = utcNow
        };
    }

    public void Rename(string title, DateTimeOffset utcNow)
    {
        Title = NormalizeTitle(title);
        Touch(utcNow);
    }

    public void UpdateCoordinates(decimal latitude, decimal longitude, DateTimeOffset utcNow)
    {
        Latitude = NormalizeLatitude(latitude);
        Longitude = NormalizeLongitude(longitude);
        Touch(utcNow);
    }

    public void UpdateAddress(
        string address,
        DateTimeOffset utcNow,
        string? city = null,
        string? district = null)
    {
        Address = NormalizeAddress(address);
        City = NormalizeOptionalText(city, 100, "City");
        District = NormalizeOptionalText(district, 100, "District");
        Touch(utcNow);
    }

    public void MarkAsDefault(DateTimeOffset utcNow)
    {
        if (IsDefault)
        {
            return;
        }

        IsDefault = true;
        Touch(utcNow);
    }

    public void RemoveDefaultStatus(DateTimeOffset utcNow)
    {
        if (!IsDefault)
        {
            return;
        }

        IsDefault = false;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        var normalized = title.Trim();

        if (normalized.Length > 100)
        {
            throw new DomainException("Title cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static string NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new DomainException("Address is required.");
        }

        return address.Trim();
    }

    private static decimal NormalizeLatitude(decimal latitude)
    {
        if (latitude is < -90m or > 90m)
        {
            throw new DomainException("Latitude must be between -90 and 90.");
        }

        return decimal.Round(latitude, 6, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeLongitude(decimal longitude)
    {
        if (longitude is < -180m or > 180m)
        {
            throw new DomainException("Longitude must be between -180 and 180.");
        }

        return decimal.Round(longitude, 6, MidpointRounding.AwayFromZero);
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
