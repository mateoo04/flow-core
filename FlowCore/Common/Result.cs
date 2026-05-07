namespace FlowCore.Common;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict
}

public readonly record struct ResultError(ErrorKind Kind, string Message);

public readonly record struct Result<T>
{
    private Result(T? value, ResultError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public ResultError? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Ok(T value) => new(value, null);
    public static Result<T> Fail(ErrorKind kind, string message) => new(default, new ResultError(kind, message));
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    public static Result<T> Validation<T>(string message) => Result<T>.Fail(ErrorKind.Validation, message);

    public static Result<T> NotFound<T>(string message) => Result<T>.Fail(ErrorKind.NotFound, message);

    public static Result<T> Conflict<T>(string message) => Result<T>.Fail(ErrorKind.Conflict, message);
}
