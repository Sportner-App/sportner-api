using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.GetPublicProfile;

public sealed record GetPublicProfileQuery(Guid UserId) : IQuery<PublicProfileResponse>;
