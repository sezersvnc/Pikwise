using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pikwise.Application.Products.Exceptions;
using Pikwise.Application.Favorites.Exceptions;
using Pikwise.Application.Users.Exceptions;
using Pikwise.Application.Recommendations.Exceptions;
using Pikwise.Application.RequirementParsing.Exceptions;
using Pikwise.Application.Explanations.Exceptions;

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
            RequirementExtractionInvalidException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway, Title = "The language model returned unusable criteria. Rephrase or enter the criteria manually."
            },
            RequirementExtractionUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable, Title = "Natural-language criteria parsing is currently unavailable. Enter the criteria manually."
            },
            ExplanationInvalidException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway, Title = "The language model returned an explanation that failed the fact check. The recommendation itself is still available."
            },
            ExplanationUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable, Title = "Explanations are currently unavailable. The recommendation itself is still available."
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
        // Language model failures are logged by reason only; the user's text is never logged.
        else if (exception is RequirementExtractionInvalidException or RequirementExtractionUnavailableException
                 or ExplanationInvalidException or ExplanationUnavailableException)
            logger.LogWarning("Language model request failed: {Reason} Trace: {TraceId}", exception.Message, context.TraceIdentifier);
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync((object)problem, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
