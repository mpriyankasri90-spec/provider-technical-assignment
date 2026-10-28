namespace Candidate.API.Domain;

public class License : IAuditable, ISoftDeletable
{
    public int LicenseId { get; set; }
    public int ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public string LicenseNumber { get; set; } = string.Empty;
    public LicenseStatus LicenseStatus { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly ExpirationDate { get; set; }

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
