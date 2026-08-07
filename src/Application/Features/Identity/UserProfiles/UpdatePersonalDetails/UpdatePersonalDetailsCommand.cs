using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdatePersonalDetails;

public sealed record UpdatePersonalDetailsCommand(short? Gender, DateOnly? BirthDate)
    : ICommand<MyProfileResponse>;
