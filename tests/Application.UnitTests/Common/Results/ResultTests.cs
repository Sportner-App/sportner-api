using FluentAssertions;
using Sportner.Application.Common.Results;

namespace Sportner.Application.UnitTests.Common.Results;

public class ResultTests
{
    [Fact]
    public void Success_HasNoErrors()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_PreservesTypedError()
    {
        var error = Error.NotFound("Entity.NotFound", "Entity was not found.");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
        result.Errors[0].Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Failure_WithoutErrors_IsRejected()
    {
        var action = () => Result.Failure([]);

        action.Should().Throw<ArgumentException>();
    }
}
