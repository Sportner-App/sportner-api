namespace Sportner.Application.Common.Results;

public class Result<T> : Result
{
    private Result(T? value, bool isSuccess, IReadOnlyList<string> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) =>
        new(value, true, Array.Empty<string>());

    public static new Result<T> Failure(params string[] errors) =>
        new(default, false, errors);

    public static new Result<T> Failure(IEnumerable<string> errors) =>
        new(default, false, errors.ToList().AsReadOnly());
}
