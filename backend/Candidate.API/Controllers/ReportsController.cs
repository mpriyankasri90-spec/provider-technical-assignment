using Candidate.API.Contracts;
using Candidate.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

/// <summary>The required license scenarios. Soft-deleted providers are excluded from every report.</summary>
[Route("api/reports")]
public sealed class ReportsController(ReportService reports) : ApiControllerBase
{
    /// <summary>Active providers and their currently valid licenses.</summary>
    [HttpGet("active-providers-active-licenses")]
    [ProducesResponseType<IReadOnlyList<ProviderLicensesDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProviderLicensesDto>>> ActiveProvidersWithActiveLicenses(
        CancellationToken cancellationToken) =>
        Ok(await reports.GetActiveProvidersWithActiveLicensesAsync(cancellationToken));

    /// <summary>Providers with status Active that hold licenses past their expiration date.</summary>
    [HttpGet("active-providers-expired-licenses")]
    [ProducesResponseType<IReadOnlyList<ProviderLicensesDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProviderLicensesDto>>> ActiveProvidersWithExpiredLicenses(
        CancellationToken cancellationToken) =>
        Ok(await reports.GetActiveProvidersWithExpiredLicensesAsync(cancellationToken));
}
