using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserProfiles.GetProfileByUsername;

public sealed record GetProfileByUsernameQuery(string Username) : IQuery<PublicProfileResponse>;
