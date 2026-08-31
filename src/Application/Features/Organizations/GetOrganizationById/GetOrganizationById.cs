using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Organizations.GetOrganizationById;

public sealed record GetOrganizationByIdQuery(Guid OrganizationId) : IQuery<OrganizationDetailResponse>;

internal sealed class GetOrganizationByIdQueryHandler
    : IQueryHandler<GetOrganizationByIdQuery, OrganizationDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetOrganizationByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<OrganizationDetailResponse>> Handle(
        GetOrganizationByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        var response = await OrganizationQueries.GetDetailAsync(
            _dbContext,
            request.OrganizationId,
            userId,
            cancellationToken);

        return response is null
            ? Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFound)
            : Result<OrganizationDetailResponse>.Success(response);
    }
}
