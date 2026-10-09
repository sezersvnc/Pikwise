namespace Pikwise.Api.Cors;

// Lets the browser frontend (another origin) call the API (ADR-029). Origins come from
// Cors:AllowedOrigins per environment; none configured means no cross-origin access.
// Tokens travel in the Authorization header, so credentials (cookies) are not allowed.
public static class FrontendCors
{
    public const string PolicyName = "frontend";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https") ||
                uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || origin.EndsWith('/'))
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins entries must be origins such as https://app.example.com (no wildcard, path or trailing slash).");
        }

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Authorization", "Content-Type")
            // Without this the browser hides Retry-After, and the 429 message cannot show the wait time.
            .WithExposedHeaders("Retry-After", "Location")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));
        return services;
    }
}
