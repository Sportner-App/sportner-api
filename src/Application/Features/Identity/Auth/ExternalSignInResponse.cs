using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Application.Features.Identity.Auth;

public sealed record ExternalSignInResponse(
    bool RequiresRegistration,
    AuthenticationResponse? Authentication,
    string? RegistrationToken,
    DateTimeOffset? RegistrationTokenExpiresAt,
    string? SuggestedUsername,
    string? FirstName,
    string? LastName,
    string? Email,
    string? ProfileImageUrl)
{
    public static ExternalSignInResponse SignedIn(AuthenticationResponse authentication) =>
        new(false, authentication, null, null, null, null, null, null, null);

    public static ExternalSignInResponse RegistrationRequired(
        ExternalRegistrationToken token,
        string suggestedUsername,
        ExternalRegistrationTicket ticket) =>
        new(
            true,
            null,
            token.Token,
            token.ExpiresAt,
            suggestedUsername,
            ticket.FirstName,
            ticket.LastName,
            ticket.Email,
            ticket.ProfileImageUrl);
}
