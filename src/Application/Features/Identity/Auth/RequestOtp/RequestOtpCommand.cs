using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.RequestOtp;

public sealed record RequestOtpCommand(string PhoneNumber) : ICommand;
