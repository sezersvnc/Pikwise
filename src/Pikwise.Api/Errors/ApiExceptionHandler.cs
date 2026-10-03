using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pikwise.Application.Products.Exceptions;

namespace Pikwise.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        // Translate known Application failures into consistent HTTP error responses.
        ProblemDetails problem = exception switch
        {
            ProductValidationException validation => new ValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest, Title = "Product validation failed."
            },
            PersistenceConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict, Title = "The data changed or conflicts with a related record. Reload and retry."
            },
            _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "An unexpected error occurred." }
        };
        // Keep unexpected exception details in logs; traceId links the response to the log entry.
        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled request error. Trace: {TraceId}", context.TraceIdentifier);
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync((object)problem, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
