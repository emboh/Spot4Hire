namespace Spot4Hire.Backend.Common;

public enum ResultStatus
{
    Success,
    NotFound,
    Invalid,
    Conflict,
    Forbidden,
}

// Carries the outcome of a service operation so the controller can map it to
// the right HTTP status without the service knowing anything about HTTP.
public class Result
{
    public ResultStatus Status { get; }

    public string? Error { get; }

    public bool IsSuccess => Status == ResultStatus.Success;

    protected Result(ResultStatus status, string? error)
    {
        Status = status;
        Error = error;
    }

    public static Result Success() => new(ResultStatus.Success, null);

    public static Result NotFound(string? error = null) => new(ResultStatus.NotFound, error);

    public static Result Invalid(string error) => new(ResultStatus.Invalid, error);

    public static Result Conflict(string error) => new(ResultStatus.Conflict, error);

    public static Result Forbidden(string? error = null) => new(ResultStatus.Forbidden, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(ResultStatus.Success, null) => Value = value;

    private Result(ResultStatus status, string? error) : base(status, error) { }

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> NotFound(string? error = null) => new(ResultStatus.NotFound, error);

    public new static Result<T> Invalid(string error) => new(ResultStatus.Invalid, error);

    public new static Result<T> Conflict(string error) => new(ResultStatus.Conflict, error);

    public new static Result<T> Forbidden(string? error = null) => new(ResultStatus.Forbidden, error);
}
