namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// A verified identity assertion from an external provider (Google/Apple), reduced to the
/// claims we actually need: their stable subject id and, when the provider supplied it, email.
/// </summary>
public sealed record ExternalIdentity(
    string ProviderUserId,
    string? Email,
    string? FirstName = null,
    string? LastName = null,
    string? ProfileImageUrl = null);
