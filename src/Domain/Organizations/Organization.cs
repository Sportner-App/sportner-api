using System.Security.Cryptography;
using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Organizations;

public class Organization : AggregateRoot
{
    public const int NameMaxLength = 80;
    public const int DescriptionMaxLength = 1000;
    public const int ImageUrlMaxLength = 2048;
    public const int InviteCodeLength = 8;
    private const string InviteAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    private Organization()
    {
    }

    public Guid FounderUserId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid? CityId { get; private set; }

    public string InviteCode { get; private set; } = null!;

    /// <summary>Herkese açık kulüp listelenir ve kodsuz katılıma açıktır.</summary>
    public bool IsPublic { get; private set; }

    public string? ImageUrl { get; private set; }

    /// <summary>Soft delete: dolu ise kulüp silinmiştir ve global query filter ile gizlenir.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public OrganizationEventCreationPolicy EventCreationPolicy { get; private set; } =
        OrganizationEventCreationPolicy.AllMembers;

    public static Organization Create(
        Guid founderUserId,
        string name,
        string? description,
        Guid? cityId,
        string inviteCode,
        bool isPublic,
        DateTimeOffset utcNow)
    {
        if (founderUserId == Guid.Empty)
        {
            throw new DomainException("Founder user id is required.");
        }

        if (cityId is { } id && id == Guid.Empty)
        {
            throw new DomainException("City id is invalid.");
        }

        return new Organization
        {
            Id = Guid.NewGuid(),
            FounderUserId = founderUserId,
            Name = NormalizeName(name),
            Description = NormalizeDescription(description),
            CityId = cityId,
            InviteCode = NormalizeInviteCode(inviteCode),
            IsPublic = isPublic,
            CreatedAt = utcNow
        };
    }

    public void UpdateDetails(
        string name,
        string? description,
        Guid? cityId,
        bool isPublic,
        DateTimeOffset utcNow)
    {
        if (cityId is { } id && id == Guid.Empty)
        {
            throw new DomainException("City id is invalid.");
        }

        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        CityId = cityId;
        IsPublic = isPublic;
        Touch(utcNow);
    }

    public void ChangeImage(string? imageUrl, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            ImageUrl = null;
        }
        else
        {
            var normalized = imageUrl.Trim();
            if (normalized.Length > ImageUrlMaxLength)
            {
                throw new DomainException(
                    $"Organization image url cannot exceed {ImageUrlMaxLength} characters.");
            }

            ImageUrl = normalized;
        }

        Touch(utcNow);
    }

    public void ChangeEventCreationPolicy(OrganizationEventCreationPolicy policy, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(policy))
        {
            throw new DomainException("Event creation policy is invalid.");
        }

        EventCreationPolicy = policy;
        Touch(utcNow);
    }

    public void TransferFounder(Guid newFounderUserId, DateTimeOffset utcNow)
    {
        if (newFounderUserId == Guid.Empty)
        {
            throw new DomainException("Founder user id is required.");
        }

        FounderUserId = newFounderUserId;
        Touch(utcNow);
    }

    public void MarkDeleted(DateTimeOffset utcNow)
    {
        if (DeletedAt is not null)
        {
            throw new DomainException("Organization is already deleted.");
        }

        DeletedAt = utcNow;
        Touch(utcNow);
    }

    public void RotateInviteCode(string inviteCode, DateTimeOffset utcNow)
    {
        InviteCode = NormalizeInviteCode(inviteCode);
        Touch(utcNow);
    }

    public static string NewInviteCode()
    {
        var chars = new char[InviteCodeLength];
        for (var index = 0; index < InviteCodeLength; index++)
        {
            chars[index] = InviteAlphabet[RandomNumberGenerator.GetInt32(InviteAlphabet.Length)];
        }

        return new string(chars);
    }

    public static string NormalizeInviteCode(string inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            throw new DomainException("Invite code is required.");
        }

        var normalized = inviteCode.Trim().ToUpperInvariant();
        if (normalized.Length != InviteCodeLength)
        {
            throw new DomainException($"Invite code must be {InviteCodeLength} characters.");
        }

        foreach (var character in normalized)
        {
            if (!InviteAlphabet.Contains(character))
            {
                throw new DomainException("Invite code contains an invalid character.");
            }
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Organization name is required.");
        }

        var normalized = name.Trim();
        if (normalized.Length > NameMaxLength)
        {
            throw new DomainException($"Organization name cannot exceed {NameMaxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();
        if (normalized.Length > DescriptionMaxLength)
        {
            throw new DomainException(
                $"Organization description cannot exceed {DescriptionMaxLength} characters.");
        }

        return normalized;
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }
}
