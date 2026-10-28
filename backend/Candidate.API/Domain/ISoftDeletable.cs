namespace Candidate.API.Domain;

/// <summary>
/// Marks an entity that must never be physically deleted.
/// Deletes are converted to updates by <c>SoftDeleteInterceptor</c> and hidden by global query filters.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
    string? DeleteReason { get; set; }
}
