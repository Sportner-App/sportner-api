using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Users;

public class UserSport : AuditableEntity
{
    private UserSport()
    {
    }

    public Guid UserId { get; private set; }

    public Guid SportId { get; private set; }

    public SkillLevel SkillLevel { get; private set; }

    public bool IsPrimary { get; private set; }

    public static UserSport Create(
        Guid userId,
        Guid sportId,
        SkillLevel skillLevel,
        DateTimeOffset utcNow,
        bool isPrimary = false)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (sportId == Guid.Empty)
        {
            throw new DomainException("Sport id is required.");
        }

        EnsureDefinedSkillLevel(skillLevel);

        return new UserSport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SportId = sportId,
            SkillLevel = skillLevel,
            IsPrimary = isPrimary,
            CreatedAt = utcNow
        };
    }

    public void ChangeSkillLevel(SkillLevel skillLevel, DateTimeOffset utcNow)
    {
        EnsureDefinedSkillLevel(skillLevel);

        if (SkillLevel == skillLevel)
        {
            return;
        }

        SkillLevel = skillLevel;
        Touch(utcNow);
    }

    public void MarkAsPrimary(DateTimeOffset utcNow)
    {
        if (IsPrimary)
        {
            return;
        }

        IsPrimary = true;
        Touch(utcNow);
    }

    public void RemovePrimaryStatus(DateTimeOffset utcNow)
    {
        if (!IsPrimary)
        {
            return;
        }

        IsPrimary = false;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static void EnsureDefinedSkillLevel(SkillLevel skillLevel)
    {
        if (!Enum.IsDefined(skillLevel))
        {
            throw new DomainException("Skill level is invalid.");
        }
    }
}
