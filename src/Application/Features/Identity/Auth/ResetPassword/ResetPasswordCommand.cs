using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : ICommand;
