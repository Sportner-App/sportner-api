namespace Sportner.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new ArgumentException("Successful result cannot contain errors.", nameof(errors));
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new ArgumentException("Failed result must contain at least one error.", nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<string> Errors { get; }

    public static Result Success() => new(true, Array.Empty<string>());

    public static Result Failure(params string[] errors) =>
        new(false, errors);

    public static Result Failure(IEnumerable<string> errors) =>
        new(false, errors.ToList().AsReadOnly());
}
