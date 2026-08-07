using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.GetProfileByUsername;

public sealed record GetProfileByUsernameQuery(string Username) : IQuery<PublicProfileResponse>;
