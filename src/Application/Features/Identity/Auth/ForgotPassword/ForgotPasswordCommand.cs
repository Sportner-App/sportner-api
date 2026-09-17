using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
