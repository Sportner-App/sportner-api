using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserProfiles.GetMyProfile;

public sealed record GetMyProfileQuery : IQuery<MyProfileResponse>;
