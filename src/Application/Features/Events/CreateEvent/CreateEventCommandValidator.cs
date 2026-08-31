using FluentValidation;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.CreateEvent;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(command => command.SportId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(150);
        RuleFor(command => command.DurationMinutes).GreaterThan(0);
        RuleFor(command => command.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(command => command.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(command => command.Address).NotEmpty();
        RuleFor(command => command.MaxParticipants)
            .GreaterThan(0)
            .When(command => command.MaxParticipants is not null);
        RuleFor(command => command.MinParticipantAge).InclusiveBetween(13, 120);
        RuleFor(command => command.MaxParticipantAge).InclusiveBetween(13, 120);
        RuleFor(command => command.MaxParticipantAge)
            .GreaterThanOrEqualTo(command => command.MinParticipantAge);
        RuleFor(command => command.SkillLevel)
            .Must(level => level is not null && Enum.IsDefined((SkillLevel)level.Value))
            .When(command => command.SkillLevel is not null)
            .WithMessage("Skill level is invalid.");
        RuleFor(command => command.FeeAmount)
            .NotNull()
            .GreaterThan(0)
            .LessThanOrEqualTo(Event.MaxFeeAmount)
            .When(command => command.IsPaid)
            .WithMessage("Fee amount is required and must be greater than zero for paid events.");
    }
}
