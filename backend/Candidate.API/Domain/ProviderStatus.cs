namespace Candidate.API.Domain;

/// <summary>
/// Business/lifecycle status of a provider. Stored as text so raw SQL stays readable.
/// Provider status never implies license validity.
/// </summary>
public enum ProviderStatus
{
    Active,
    Inactive,
    Pending
}
