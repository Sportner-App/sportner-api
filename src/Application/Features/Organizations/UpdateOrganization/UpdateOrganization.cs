using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Organizations;

namespace Sportner.Application.Features.Organizations.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    Guid OrganizationId,
    string Name,
    string? Description,
    Guid? CityId) : ICommand<OrganizationDetailResponse>;

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
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

internal sealed class UpdateOrganizationCommandHandler
    : ICommandHandler<UpdateOrganizationCommand, OrganizationDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateOrganizationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrganizationDetailResponse>> Handle(
        UpdateOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        var membership = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            userId,
            cancellationToken);

        if (membership is null || !membership.CanManageMembers)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CannotManageMembers);
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

        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(candidate => candidate.Id == request.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFound);
        }

        try
        {
            organization.UpdateDetails(
                request.Name,
                request.Description,
                request.CityId,
                _timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CannotCreateContent);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await OrganizationQueries.GetDetailAsync(
            _dbContext,
            organization.Id,
            userId,
            cancellationToken);

        return Result<OrganizationDetailResponse>.Success(response!);
    }
}
