using System.ComponentModel.DataAnnotations;
using Candidate.API.Contracts;
using Candidate.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

[Route("api/dashboard")]
public sealed class DashboardController(DashboardService dashboard) : ApiControllerBase
{
    /// <summary>Provider and license metrics for the read-only dashboard.</summary>
    /// <param name="expiringWithinDays">Window for the "expiring soon" list (1–365 days, default 60).</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(
        [FromQuery, Range(1, 365)] int expiringWithinDays = 60, CancellationToken cancellationToken = default) =>
        Ok(await dashboard.GetSummaryAsync(expiringWithinDays, cancellationToken));
}
