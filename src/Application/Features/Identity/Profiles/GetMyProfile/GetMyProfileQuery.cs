using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.GetMyProfile;

public sealed record GetMyProfileQuery : IQuery<MyProfileResponse>;
