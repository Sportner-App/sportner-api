using FluentAssertions;
using Sportner.Application.Features.Reviews.CreateReview;

namespace Sportner.Application.UnitTests.Features.Reviews;

public sealed class CreateReviewCommandValidatorTests
{
    private readonly CreateReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        var command = new CreateReviewCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Rating: 5,
            Comment: "Harika bir oyuncu.");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)6)]
    public void Validate_Fails_ForOutOfRangeRating(short rating)
    {
        var command = new CreateReviewCommand(Guid.NewGuid(), Guid.NewGuid(), rating, null);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenEventIdIsEmpty()
    {
        var command = new CreateReviewCommand(Guid.Empty, Guid.NewGuid(), 4, null);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
