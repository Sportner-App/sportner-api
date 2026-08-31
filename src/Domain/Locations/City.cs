using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Locations;

public sealed class City : AggregateRoot
{
    private City()
    {
    }

    public short PlateCode { get; private set; }

    public string Name { get; private set; } = null!;

    public static City Create(short plateCode, string name, DateTimeOffset utcNow)
    {
        return new City
        {
            Id = Guid.NewGuid(),
            PlateCode = NormalizePlateCode(plateCode),
            Name = NormalizeName(name),
            CreatedAt = utcNow
        };
    }

    public void Rename(string name, DateTimeOffset utcNow)
    {
        var normalizedName = NormalizeName(name);
        if (string.Equals(Name, normalizedName, StringComparison.Ordinal))
        {
            return;
        }

        Name = normalizedName;
        UpdatedAt = utcNow;
    }

    private static short NormalizePlateCode(short plateCode)
    {
        if (plateCode is < 1 or > 81)
        {
            throw new DomainException("City plate code must be between 1 and 81.");
        }

        return plateCode;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("City name is required.");
        }

        var normalized = name.Trim();
        if (normalized.Length > 100)
        {
            throw new DomainException("City name cannot exceed 100 characters.");
        }

        return normalized;
    }
}
