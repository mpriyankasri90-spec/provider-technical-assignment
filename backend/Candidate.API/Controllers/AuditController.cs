using Candidate.API.Contracts;
using Candidate.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

/// <summary>
/// Audit and maintenance endpoints that can see soft-deleted data. In a real deployment these
/// would require an administrator/auditor role; authentication is out of scope here.
/// </summary>
[Route("api/audit/providers")]
public sealed class AuditController(AuditService audit) : ApiControllerBase
{
    /// <summary>Lists soft-deleted providers with who deleted them, when and why.</summary>
    [HttpGet("deleted")]
    [ProducesResponseType<IReadOnlyList<DeletedProviderDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeletedProviderDto>>> ListDeleted(CancellationToken cancellationToken) =>
        Ok(await audit.ListDeletedProvidersAsync(cancellationToken));

    /// <summary>Gets the full audit record of any provider, including deleted providers and licenses.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProviderAuditDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderAuditDto>> Get(int id, CancellationToken cancellationToken)
    {
        var provider = await audit.GetProviderAsync(id, cancellationToken);
        return provider is null ? NotFoundProblem($"Provider {id} was not found.") : Ok(provider);
    }

    /// <summary>Restores a soft-deleted provider.</summary>
    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var result = await audit.RestoreProviderAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ProblemFor(result.Error!);
    }
}
