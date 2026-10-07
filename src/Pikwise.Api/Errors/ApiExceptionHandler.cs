using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pikwise.Application.Products.Exceptions;
using Pikwise.Application.Favorites.Exceptions;
using Pikwise.Application.Users.Exceptions;
using Pikwise.Application.Recommendations.Exceptions;

namespace Pikwise.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        // Translate known Application failures into consistent HTTP error responses.
        ProblemDetails problem = exception switch
        {
            ProductsNotFoundException missing => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound, Title = "One or more requested products do not exist.",
                Extensions = { ["missingProductIds"] = missing.MissingProductIds }
            },
            UserProfileUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden, Title = "A local user profile could not be resolved."
            },
            FavoriteAlreadyExistsException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict, Title = "This product is already in your favorites."
            },
            ProductValidationException validation => new ValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest, Title = "Product validation failed."
            },
            RecommendationValidationException validation => new ValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest, Title = "Recommendation request validation failed."
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
