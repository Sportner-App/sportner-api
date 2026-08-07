using FluentAssertions;
using Sportner.Application.Features.Social.Posts.CreatePost;

namespace Sportner.Application.UnitTests.Features.Social;

public sealed class CreatePostCommandValidatorTests
{
    private readonly CreatePostCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForTextOnly()
    {
        var command = new CreatePostCommand("Bugün harika bir antrenman oldu.", null);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenContentExceedsLimit()
    {
        var command = new CreatePostCommand(new string('a', 2201), null);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
