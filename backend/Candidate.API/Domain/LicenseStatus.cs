namespace Candidate.API.Domain;

/// <summary>
/// Recorded status of a license, as entered by staff. Expiration is decided by
/// <see cref="License.ExpirationDate"/>, so a license recorded as Active can still be expired.
/// </summary>
public enum LicenseStatus
{
    Active,
    Expired,
    Suspended
}
