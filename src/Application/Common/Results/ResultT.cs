namespace Sportner.Application.Common.Results;

public class Result<T> : Result
{
    private Result(T? value, bool isSuccess, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) =>
        new(value, true, Array.Empty<Error>());

    public static new Result<T> Failure(params Error[] errors) =>
        new(default, false, errors);

    public static new Result<T> Failure(IEnumerable<Error> errors) =>
        new(default, false, errors.ToList().AsReadOnly());
}
