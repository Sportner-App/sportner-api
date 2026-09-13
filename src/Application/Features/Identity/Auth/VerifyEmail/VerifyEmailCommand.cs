using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.VerifyEmail;

public sealed record VerifyEmailCommand(string Code) : ICommand;
