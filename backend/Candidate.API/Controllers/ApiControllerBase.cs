using Candidate.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Maps an expected business failure to an RFC 7807 problem response.</summary>
    protected ActionResult ProblemFor(ServiceError error) => error.Kind switch
    {
        ServiceErrorKind.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: error.Message),
        ServiceErrorKind.Conflict => Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: error.Message),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Kind, null)
    };

    protected ActionResult NotFoundProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: detail);
}
