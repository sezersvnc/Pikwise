using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Application.Explanations.Interfaces;
using Pikwise.Application.RequirementParsing.Interfaces;

namespace Pikwise.IntegrationTests;

// Limits are lowered through configuration; fakes answer, so no SQL Server and no real model is used.
public class LanguageModelRateLimitTests
{
    private const string CriteriaUrl = "/api/recommendations/criteria";
    private const string ExplanationUrl = "/api/recommendations/explanation";

    [Fact]
    public async Task Per_user_limit_returns_429_with_retry_after_without_affecting_other_users()
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory(settings: Limits(perUser: 1, total: 10));
        await using var factory = tokens.Configure(baseFactory, AddFakes);
        using var client = factory.CreateClient();

        using var first = await PostCriteria(client, tokens.Create());
        using var second = await PostCriteria(client, tokens.Create());
        using var otherUser = await PostCriteria(client, tokens.Create(subject: tokens.OtherSubject));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await AssertTooManyRequests(second);
        Assert.Equal(HttpStatusCode.OK, otherUser.StatusCode);
    }

    [Fact]
    public async Task Total_limit_is_shared_by_all_users()
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory(settings: Limits(perUser: 5, total: 2));
        await using var factory = tokens.Configure(baseFactory, AddFakes);
        using var client = factory.CreateClient();

        using var first = await PostCriteria(client, tokens.Create());
        using var second = await PostCriteria(client, tokens.Create(subject: tokens.OtherSubject));
        using var third = await PostCriteria(client, tokens.Create(subject: Guid.NewGuid().ToString()));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        await AssertTooManyRequests(third);
    }

    [Fact]
    public async Task Both_language_model_endpoints_share_the_per_user_limit()
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory(settings: Limits(perUser: 1, total: 10));
        await using var factory = tokens.Configure(baseFactory, AddFakes);
        using var client = factory.CreateClient();

        using var criteria = await PostCriteria(client, tokens.Create());
        using var request = new HttpRequestMessage(HttpMethod.Post, ExplanationUrl) { Content = JsonContent.Create(new { minRamGb = 16 }) };
        request.Headers.Authorization = new("Bearer", tokens.Create());
        using var explanation = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, criteria.StatusCode);
        await AssertTooManyRequests(explanation);
    }

    [Fact]
    public async Task Recommendation_endpoint_is_not_rate_limited()
    {
        await using var factory = new PikwiseApiFactory(settings: Limits(perUser: 1, total: 1));
        using var client = factory.CreateClient();

        // Invalid bodies answer 400 before any database access; a limit would answer 429 instead.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/recommendations", new { minRamGb = 0 });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    private static Dictionary<string, string> Limits(int perUser, int total) => new()
    {
        ["RateLimiting:LanguageModel:PerUserPerMinute"] = perUser.ToString(),
        ["RateLimiting:LanguageModel:TotalPerMinute"] = total.ToString()
    };

    private static void AddFakes(IServiceCollection services)
    {
        services.AddSingleton<IRequirementExtractor>(new FakeExtractor());
        services.AddSingleton<IExplanationGenerator>(new FakeGenerator());
    }

    private static async Task<HttpResponseMessage> PostCriteria(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, CriteriaUrl) { Content = JsonContent.Create(new { text = "16 GB RAM" }) };
        request.Headers.Authorization = new("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task AssertTooManyRequests(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.RetryAfter is not null, "Retry-After header is missing.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(429, json.RootElement.GetProperty("status").GetInt32());
    }

    private sealed class FakeExtractor : IRequirementExtractor
    {
        public Task<string> ExtractAsync(string text, CancellationToken cancellationToken) =>
            Task.FromResult("""{ "minRamGb": 16, "importance": null, "unsupported": [] }""");
    }

    // Never reached in these tests: the explanation call is rejected by the limiter, and an
    // empty ranking (no SQL data) would not call the model anyway.
    private sealed class FakeGenerator : IExplanationGenerator
    {
        public Task<string> GenerateAsync(string inputJson, CancellationToken cancellationToken) =>
            Task.FromResult("""{ "products": [], "valueComment": null }""");
    }
}
