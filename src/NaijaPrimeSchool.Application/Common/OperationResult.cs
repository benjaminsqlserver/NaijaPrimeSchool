namespace NaijaPrimeSchool.Application.Common;

public class OperationResult
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static OperationResult Success() => new() { Succeeded = true };

    public static OperationResult Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };

    public static OperationResult Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToArray() };
}

public class OperationResult<T> : OperationResult
{
    public T? Data { get; init; }

    public static OperationResult<T> Success(T data) =>
        new() { Succeeded = true, Data = data };

    public static new OperationResult<T> Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };

    public static new OperationResult<T> Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToArray() };
}
