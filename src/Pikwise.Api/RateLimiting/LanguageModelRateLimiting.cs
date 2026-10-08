using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Pikwise.Api.RateLimiting;

// Limits the endpoints that call the language model (ADR-026/028): a per-user window protects
// against one account looping, and a shared total keeps all users under the provider's free quota.
public static class LanguageModelRateLimiting
{
    public const string PolicyName = "language-model";
    public const int DefaultPerUserPerMinute = 5;
    public const int DefaultTotalPerMinute = 25;

    public static IServiceCollection AddLanguageModelRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting:LanguageModel");
        var perUser = section.GetValue("PerUserPerMinute", DefaultPerUserPerMinute);
        var total = section.GetValue("TotalPerMinute", DefaultTotalPerMinute);
        if (perUser < 1 || total < 1)
            throw new InvalidOperationException("RateLimiting:LanguageModel limits must be at least 1.");

        services.AddRateLimiter(options =>
        {
            // The global limiter runs for every request but only counts language model endpoints.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == PolicyName
                    ? RateLimitPartition.GetFixedWindowLimiter("language-model-total", _ => PerMinute(total))
                    : RateLimitPartition.GetNoLimiter("other"));
            // The endpoints require authentication, so the Supabase subject identifies the user.
            options.AddPolicy(PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst("sub")?.Value ?? string.Empty, _ => PerMinute(perUser)));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many language model requests. Try again later.",
                    Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier }
                };
                await response.WriteAsJsonAsync((object)problem, options: null,
                    contentType: "application/problem+json", cancellationToken: cancellationToken);
            };
        });
        return services;
    }

    private static FixedWindowRateLimiterOptions PerMinute(int permits) => new()
    {
        PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
    };
}
