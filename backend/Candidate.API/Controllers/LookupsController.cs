using Candidate.API.Contracts;
using Candidate.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

[Route("api/lookups")]
public sealed class LookupsController(ProviderQueryService queries) : ApiControllerBase
{
    /// <summary>Status values and known counties for forms and filters.</summary>
    [HttpGet]
    [ProducesResponseType<LookupsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LookupsDto>> Get(CancellationToken cancellationToken) =>
        Ok(await queries.GetLookupsAsync(cancellationToken));
}
