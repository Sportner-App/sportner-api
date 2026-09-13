using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.DeleteAccount;

/// <summary>
/// Self-service account deletion for the current user. Soft delete only — flips
/// <c>User.Status</c> to <c>Deleted</c> and revokes active sessions; nothing is
/// physically removed.
/// </summary>
public sealed record DeleteAccountCommand : ICommand;
