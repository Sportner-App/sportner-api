using FluentAssertions;
using Sportner.Application.Features.Moderation.CreateReport;

namespace Sportner.Application.UnitTests.Features.Moderation;

public sealed class CreateReportCommandValidatorTests
{
    private readonly CreateReportCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForDefinedEntityType()
    {
        var command = new CreateReportCommand(2, Guid.NewGuid(), Guid.NewGuid(), "spam");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_ForUndefinedEntityType()
    {
        var command = new CreateReportCommand(99, Guid.NewGuid(), Guid.NewGuid(), null);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
