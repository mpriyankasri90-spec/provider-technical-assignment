namespace Candidate.API.Domain;

public class Provider : IAuditable, ISoftDeletable
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public ProviderStatus Status { get; set; }

    public List<License> Licenses { get; set; } = new();

    // IAuditable
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public int Version { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeleteReason { get; set; }
}
