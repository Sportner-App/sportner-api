using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.UpdatePersonalDetails;

public sealed record UpdatePersonalDetailsCommand(short? Gender, DateOnly? BirthDate)
    : ICommand<MyProfileResponse>;
