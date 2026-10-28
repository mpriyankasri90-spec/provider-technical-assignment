using Candidate.API.Contracts;
using Candidate.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

/// <summary>Standard provider workflows. Soft-deleted providers are never returned or modified here.</summary>
[Route("api/providers")]
public sealed class ProvidersController(ProviderQueryService queries, ProviderService providers) : ApiControllerBase
{
    /// <summary>Lists non-deleted providers with license counts. Supports search, filters and paging.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProviderListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProviderListItemDto>>> List(
        [FromQuery] ProviderListQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.ListAsync(query, cancellationToken));

    /// <summary>Gets a provider with its licenses and computed license validity.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProviderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var provider = await queries.GetAsync(id, cancellationToken);
        return provider is null ? NotFoundProblem($"Provider {id} was not found.") : Ok(provider);
    }

    /// <summary>Gets the licenses of a provider.</summary>
    [HttpGet("{id:int}/licenses")]
    [ProducesResponseType<IReadOnlyList<LicenseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LicenseDto>>> GetLicenses(int id, CancellationToken cancellationToken)
    {
        var licenses = await queries.GetLicensesAsync(id, cancellationToken);
        return licenses is null ? NotFoundProblem($"Provider {id} was not found.") : Ok(licenses);
    }

    /// <summary>Creates a provider.</summary>
    [HttpPost]
    [ProducesResponseType<ProviderDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProviderDetailDto>> Create(
        CreateProviderRequest request, CancellationToken cancellationToken)
    {
        var result = await providers.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.ProviderId }, result.Value)
            : ProblemFor(result.Error!);
    }

    /// <summary>Updates a provider. The request must carry the <c>version</c> that was loaded.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProviderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProviderDetailDto>> Update(
        int id, UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        var result = await providers.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ProblemFor(result.Error!);
    }

    /// <summary>
    /// Soft-deletes a provider. The record stays in the database for audit and disappears from
    /// all standard listings. Hard deletion is not supported.
    /// </summary>
    /// <param name="id">Provider id.</param>
    /// <param name="reason">Optional reason, kept for audit.</param>
    /// <param name="version">Optional version the client loaded; when supplied a stale delete returns 409.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(
        int id, [FromQuery] string? reason, [FromQuery] int? version, CancellationToken cancellationToken)
    {
        if (reason?.Length > 500)
        {
            ModelState.AddModelError(nameof(reason), "Reason must be 500 characters or fewer.");
            return ValidationProblem(ModelState);
        }

        var result = await providers.SoftDeleteAsync(id, reason, version, cancellationToken);
        return result.IsSuccess ? NoContent() : ProblemFor(result.Error!);
    }
}
