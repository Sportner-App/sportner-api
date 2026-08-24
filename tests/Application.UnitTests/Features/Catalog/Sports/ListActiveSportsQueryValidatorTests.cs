using FluentAssertions;
using Sportner.Application.Features.Catalog.Sports.ListActiveSports;

namespace Sportner.Application.UnitTests.Features.Catalog.Sports;

public sealed class ListActiveSportsQueryValidatorTests
{
    private readonly ListActiveSportsQueryValidator _validator = new();

    [Fact]
    public void Validate_Passes_WhenSearchIsNullOrWhitespace()
    {
        _validator.Validate(new ListActiveSportsQuery()).IsValid.Should().BeTrue();
        _validator.Validate(new ListActiveSportsQuery(Search: "  ")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenSearchIsSingleCharacter()
    {
        var result = _validator.Validate(new ListActiveSportsQuery(Search: "a"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(ListActiveSportsQuery.Search));
    }

    [Fact]
    public void Validate_Passes_WhenSearchHasAtLeastTwoCharacters()
    {
        _validator.Validate(new ListActiveSportsQuery(Search: "ba")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenPageSizeExceedsMax()
    {
        _validator.Validate(new ListActiveSportsQuery(PageSize: 101)).IsValid.Should().BeFalse();
    }
}
