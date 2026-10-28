namespace Candidate.API.Domain;

/// <summary>
/// Audit columns maintained by <c>AuditInterceptor</c>; never set from client input.
/// <see cref="Version"/> is the optimistic-concurrency token, incremented on every update.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    string CreatedBy { get; set; }
    DateTime? ModifiedAtUtc { get; set; }
    string? ModifiedBy { get; set; }
    int Version { get; set; }
}
