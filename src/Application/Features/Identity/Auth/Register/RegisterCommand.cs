using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.Register;

public sealed record RegisterCommand(
    string Username,
    string Password,
    string FirstName,
    string? LastName,
    short? Gender,
    DateOnly BirthDate,
    string? IpAddress,
    string? UserAgent) : ICommand<AuthenticationResponse>;
