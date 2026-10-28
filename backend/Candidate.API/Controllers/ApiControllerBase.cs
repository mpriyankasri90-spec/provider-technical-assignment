using Candidate.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Maps an expected business failure to an RFC 7807 problem response. The "code" extension
    /// lets clients react to the kind of failure without parsing the message text.
    /// </summary>
    protected ActionResult ProblemFor(ServiceError error)
    {
        var (status, title) = error.Kind switch
        {
            ServiceErrorKind.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ServiceErrorKind.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Kind, null)
        };

        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, title, detail: error.Message);
        problem.Extensions["code"] = error.Code;
        return new ObjectResult(problem) { StatusCode = status };
    }

    protected ActionResult NotFoundProblem(string detail) =>
        ProblemFor(new ServiceError(ServiceErrorKind.NotFound, ErrorCodes.NotFound, detail));
}
