using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Sessions.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId) : ICommand;
