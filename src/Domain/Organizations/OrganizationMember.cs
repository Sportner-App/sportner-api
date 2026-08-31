using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Organizations;

public class OrganizationMember : AuditableEntity
{
    private OrganizationMember()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public OrganizationRole Role { get; private set; }

    public OrganizationMemberStatus Status { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    public bool IsApproved => Status is OrganizationMemberStatus.Approved;

    public bool CanManageMembers =>
        IsApproved && Role is OrganizationRole.Founder or OrganizationRole.Admin;

    public bool CanCreateEvents => IsApproved;

    public static OrganizationMember CreateFounder(
        Guid organizationId,
        Guid userId,
        DateTimeOffset utcNow)
    {
        EnsureIds(organizationId, userId);

        return new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Role = OrganizationRole.Founder,
            Status = OrganizationMemberStatus.Approved,
            RespondedAt = utcNow,
            CreatedAt = utcNow
        };
    }

    public static OrganizationMember CreatePending(
        Guid organizationId,
        Guid userId,
        DateTimeOffset utcNow)
    {
        EnsureIds(organizationId, userId);

        return new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Role = OrganizationRole.Member,
            Status = OrganizationMemberStatus.Pending,
            CreatedAt = utcNow
        };
    }

    public void Approve(DateTimeOffset utcNow)
    {
        if (Status is OrganizationMemberStatus.Approved)
        {
            return;
        }

        if (Status is not OrganizationMemberStatus.Pending)
        {
            throw new DomainException("Only pending memberships can be approved.");
        }

        Status = OrganizationMemberStatus.Approved;
        Role = OrganizationRole.Member;
        RespondedAt = utcNow;
        Touch(utcNow);
    }

    public void Reject(DateTimeOffset utcNow)
    {
        if (Status is OrganizationMemberStatus.Rejected)
        {
            return;
        }

        if (Status is not OrganizationMemberStatus.Pending)
        {
            throw new DomainException("Only pending memberships can be rejected.");
        }

        Status = OrganizationMemberStatus.Rejected;
        RespondedAt = utcNow;
        Touch(utcNow);
    }

    public void Reapply(DateTimeOffset utcNow)
    {
        if (Status is OrganizationMemberStatus.Approved)
        {
            throw new DomainException("User is already a member.");
        }

        if (Status is OrganizationMemberStatus.Pending)
        {
            throw new DomainException("A membership request is already pending.");
        }

        if (Status is OrganizationMemberStatus.Blocked)
        {
            throw new DomainException("User is blocked from this organization.");
        }

        Role = OrganizationRole.Member;
        Status = OrganizationMemberStatus.Pending;
        RespondedAt = null;
        Touch(utcNow);
    }

    public void Remove(DateTimeOffset utcNow)
    {
        if (Role is OrganizationRole.Founder)
        {
            throw new DomainException("The founder cannot be removed.");
        }

        if (Status is not OrganizationMemberStatus.Approved)
        {
            throw new DomainException("Only approved members can be removed.");
        }

        Status = OrganizationMemberStatus.Left;
        Role = OrganizationRole.Member;
        RespondedAt = utcNow;
        Touch(utcNow);
    }

    public void Block(DateTimeOffset utcNow)
    {
        if (Role is OrganizationRole.Founder)
        {
            throw new DomainException("The founder cannot be blocked.");
        }

        if (Status is OrganizationMemberStatus.Blocked)
        {
            return;
        }

        Status = OrganizationMemberStatus.Blocked;
        Role = OrganizationRole.Member;
        RespondedAt = utcNow;
        Touch(utcNow);
    }

    public void Unblock(DateTimeOffset utcNow)
    {
        if (Status is not OrganizationMemberStatus.Blocked)
        {
            throw new DomainException("Only blocked members can be unblocked.");
        }

        Status = OrganizationMemberStatus.Left;
        Role = OrganizationRole.Member;
        RespondedAt = utcNow;
        Touch(utcNow);
    }

    public bool CanModerate(OrganizationMember target)
    {
        if (!CanManageMembers || target.UserId == UserId)
        {
            return false;
        }

        if (target.Role is OrganizationRole.Founder)
        {
            return false;
        }

        return Role is not OrganizationRole.Admin || target.Role is not OrganizationRole.Admin;
    }

    public void Leave(DateTimeOffset utcNow)
    {
        if (Role is OrganizationRole.Founder)
        {
            throw new DomainException("The founder cannot leave the organization.");
        }

        if (Status is not OrganizationMemberStatus.Approved)
        {
            throw new DomainException("Only approved members can leave.");
        }

        Status = OrganizationMemberStatus.Left;
        Role = OrganizationRole.Member;
        RespondedAt = utcNow;
        Touch(utcNow);
    }

    public void SetRole(OrganizationRole role, DateTimeOffset utcNow)
    {
        if (Role is OrganizationRole.Founder)
        {
            throw new DomainException("The founder role cannot be changed.");
        }

        if (role is OrganizationRole.Founder)
        {
            throw new DomainException("Founder role cannot be assigned.");
        }

        if (Status is not OrganizationMemberStatus.Approved)
        {
            throw new DomainException("Only approved members can change role.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException("Organization role is invalid.");
        }

        Role = role;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static void EnsureIds(Guid organizationId, Guid userId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainException("Organization id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }
    }
}
