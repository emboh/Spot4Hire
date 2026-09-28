using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Spot4Hire.Backend.Common;

// A bad filter or orderBy in the query string throws a Gridify exception; map it
// to a 400 instead of letting it surface as a 500.
public sealed class GridifyExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception.GetType().FullName?.StartsWith("Gridify", StringComparison.Ordinal) != true)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid query parameter",
                Detail = exception.Message,
            },
            cancellationToken);

        return true;
    }
}
