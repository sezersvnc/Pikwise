using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Application.RequirementParsing.Interfaces;

namespace Pikwise.IntegrationTests;

// Development settings allow http://localhost:5173 and http://localhost:3000 (ADR-029). No SQL Server is used.
public class CorsTests
{
    private const string AllowedOrigin = "http://localhost:5173";

    [Fact]
    public async Task Preflight_from_an_allowed_origin_permits_the_frontend_request()
    {
        await using var factory = new PikwiseApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/recommendations/criteria");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, Header(response, "Access-Control-Allow-Origin"));
        Assert.Contains("POST", Header(response, "Access-Control-Allow-Methods"));
        Assert.Contains("authorization", Header(response, "Access-Control-Allow-Headers")!.ToLowerInvariant());
        Assert.Null(Header(response, "Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Unknown_origin_gets_no_cors_headers()
    {
        await using var factory = new PikwiseApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/recommendations");
        request.Headers.Add("Origin", "https://evil.example.test");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        Assert.Null(Header(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Error_responses_also_carry_cors_headers()
    {
        await using var factory = new PikwiseApiFactory();
        using var client = factory.CreateClient();

        // 400 from the central exception handler (validation before database access).
        using var invalid = new HttpRequestMessage(HttpMethod.Post, "/api/recommendations") { Content = JsonContent.Create(new { minRamGb = 0 }) };
        invalid.Headers.Add("Origin", AllowedOrigin);
        using var badRequest = await client.SendAsync(invalid);
        // 401 from authorization.
        using var anonymous = new HttpRequestMessage(HttpMethod.Post, "/api/recommendations/criteria") { Content = JsonContent.Create(new { text = "16 GB RAM" }) };
        anonymous.Headers.Add("Origin", AllowedOrigin);
        using var unauthorized = await client.SendAsync(anonymous);

        Assert.Equal(HttpStatusCode.BadRequest, badRequest.StatusCode);
        Assert.Equal(AllowedOrigin, Header(badRequest, "Access-Control-Allow-Origin"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(AllowedOrigin, Header(unauthorized, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Rate_limited_response_exposes_retry_after_to_the_browser()
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory(settings: new Dictionary<string, string>
        {
            ["RateLimiting:LanguageModel:PerUserPerMinute"] = "1"
        });
        await using var factory = tokens.Configure(baseFactory, services =>
            services.AddSingleton<IRequirementExtractor>(new FakeExtractor()));
        using var client = factory.CreateClient();

        using var first = await client.SendAsync(Criteria(tokens.Create()));
        using var limited = await client.SendAsync(Criteria(tokens.Create()));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(AllowedOrigin, Header(limited, "Access-Control-Allow-Origin"));
        Assert.Contains("retry-after", Header(limited, "Access-Control-Expose-Headers")!.ToLowerInvariant());
    }

    [Theory]
    [InlineData("*")]
    [InlineData("localhost:5173")]
    [InlineData("http://localhost:5173/")]
    [InlineData("http://localhost:5173/app")]
    public async Task Invalid_origin_setting_stops_startup(string origin)
    {
        await using var factory = new PikwiseApiFactory(settings: new Dictionary<string, string>
        {
            ["Cors:AllowedOrigins:0"] = origin
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.Services);

        Assert.Contains("Cors:AllowedOrigins", exception.ToString());
    }

    private static HttpRequestMessage Criteria(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/recommendations/criteria") { Content = JsonContent.Create(new { text = "16 GB RAM" }) };
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;

    private sealed class FakeExtractor : IRequirementExtractor
    {
        public Task<string> ExtractAsync(string text, CancellationToken cancellationToken) =>
            Task.FromResult("""{ "minRamGb": 16, "importance": null, "unsupported": [] }""");
    }
}
