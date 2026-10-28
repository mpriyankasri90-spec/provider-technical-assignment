namespace Candidate.API.Services;

public enum ServiceErrorKind
{
    NotFound,
    Conflict
}

/// <summary>Stable, machine-readable error codes returned to clients as the problem "code" extension.</summary>
public static class ErrorCodes
{
    public const string NotFound = "not_found";
    public const string DuplicateProvider = "duplicate_provider";
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string DataIntegrity = "data_integrity";
}

public sealed record ServiceError(ServiceErrorKind Kind, string Code, string Message);

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

    public static ServiceResult NotFound(string message) =>
        new(new ServiceError(ServiceErrorKind.NotFound, ErrorCodes.NotFound, message));

    public static ServiceResult Conflict(string code, string message) =>
        new(new ServiceError(ServiceErrorKind.Conflict, code, message));
}

public sealed class ServiceResult<T> : ServiceResult
{
    private ServiceResult(T? value, ServiceError? error) : base(error) => Value = value;

    public T? Value { get; }

    public static ServiceResult<T> Success(T value) => new(value, null);

    public static new ServiceResult<T> NotFound(string message) =>
        new(default, new ServiceError(ServiceErrorKind.NotFound, ErrorCodes.NotFound, message));

    public static new ServiceResult<T> Conflict(string code, string message) =>
        new(default, new ServiceError(ServiceErrorKind.Conflict, code, message));
}
