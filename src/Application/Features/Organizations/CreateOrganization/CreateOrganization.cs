using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Organizations;

namespace Sportner.Application.Features.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    string? Description,
    Guid? CityId) : ICommand<OrganizationDetailResponse>;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Organization.NameMaxLength);
        RuleFor(command => command.Description)
            .MaximumLength(Organization.DescriptionMaxLength)
            .When(command => command.Description is not null);
        RuleFor(command => command.CityId)
            .NotEmpty()
            .When(command => command.CityId is not null);
    }
}

internal sealed class CreateOrganizationCommandHandler
    : ICommandHandler<CreateOrganizationCommand, OrganizationDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateOrganizationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrganizationDetailResponse>> Handle(
        CreateOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.UserNotFound);
        }

        if (!user.CanCreateContent())
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CannotCreateContent);
        }

        if (request.CityId is { } cityId)
        {
            var cityExists = await _dbContext.Cities.AsNoTracking()
                .AnyAsync(city => city.Id == cityId, cancellationToken);

            if (!cityExists)
            {
                return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CityNotFound);
            }
        }

        string inviteCode;
        try
        {
            inviteCode = await OrganizationQueries.AllocateInviteCodeAsync(_dbContext, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.InviteCodeUnavailable);
        }

        var utcNow = _timeProvider.GetUtcNow();
        Organization organization;
        OrganizationMember founder;

        try
        {
            organization = Organization.Create(
                userId,
                request.Name,
                request.Description,
                request.CityId,
                inviteCode,
                utcNow);
            founder = OrganizationMember.CreateFounder(organization.Id, userId, utcNow);
        }
        catch (DomainException)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CannotCreateContent);
        }

        _dbContext.Organizations.Add(organization);
        _dbContext.OrganizationMembers.Add(founder);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await OrganizationQueries.GetDetailAsync(
            _dbContext,
            organization.Id,
            userId,
            cancellationToken);

        return Result<OrganizationDetailResponse>.Success(response!);
    }
}
