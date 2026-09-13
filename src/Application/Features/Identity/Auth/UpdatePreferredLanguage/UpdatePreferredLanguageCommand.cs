using Sportner.Application.Abstractions.Messaging;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.Auth.UpdatePreferredLanguage;

public sealed record UpdatePreferredLanguageCommand(Language Language) : ICommand;
