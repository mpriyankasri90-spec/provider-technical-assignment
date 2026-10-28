namespace Candidate.API.Services;

public enum ServiceErrorKind
{
    NotFound,
    Conflict
}

public sealed record ServiceError(ServiceErrorKind Kind, string Message);

/// <summary>
/// Outcome of a business operation. Expected failures (not found, rule conflicts) are values,
/// not exceptions; controllers map them to HTTP responses.
/// </summary>
public class ServiceResult
{
    protected ServiceResult(ServiceError? error) => Error = error;

    public ServiceError? Error { get; }
    public bool IsSuccess => Error is null;

    public static ServiceResult Success() => new(null);
    public static ServiceResult NotFound(string message) => new(new ServiceError(ServiceErrorKind.NotFound, message));
    public static ServiceResult Conflict(string message) => new(new ServiceError(ServiceErrorKind.Conflict, message));
}

public sealed class ServiceResult<T> : ServiceResult
{
    private ServiceResult(T? value, ServiceError? error) : base(error) => Value = value;

    public T? Value { get; }

    public static ServiceResult<T> Success(T value) => new(value, null);
    public static new ServiceResult<T> NotFound(string message) => new(default, new ServiceError(ServiceErrorKind.NotFound, message));
    public static new ServiceResult<T> Conflict(string message) => new(default, new ServiceError(ServiceErrorKind.Conflict, message));
}
