using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Abstractions.Authentication;

public sealed record ExternalRegistrationTicket(
    ExternalLoginProvider Provider,
    string ProviderUserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl);
