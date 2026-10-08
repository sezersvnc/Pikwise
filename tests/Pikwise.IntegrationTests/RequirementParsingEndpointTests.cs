using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Application.RequirementParsing.Interfaces;

namespace Pikwise.IntegrationTests;

// No SQL Server and no real language model: the extractor is the configured default or a fake.
public class RequirementParsingEndpointTests
{
    private const string Url = "/api/recommendations/criteria";

    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    [InlineData("signature")]
    public async Task Requires_a_valid_user_token(string token)
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = tokens.Configure(baseFactory);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = JsonContent.Create(new { text = "50 bin TL" }) };
        if (token != "missing") request.Headers.Authorization = new("Bearer", tokens.Create(token));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Without_a_configured_provider_returns_503_problem_details()
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, output: null);

        using var response = await client.PostAsJsonAsync(Url, new { text = "50 bin TL bütçem var" });

        await AssertProblem(response, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Valid_model_output_returns_criteria_in_the_recommendation_request_shape()
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, """
            { "budgetMax": 50000, "minRamGb": 16, "minStorageGb": null, "maxWeightKg": null,
              "importance": { "ram": null, "storage": null, "cpu": 4, "gpu": 3, "weight": 4, "refreshRate": null },
              "unsupported": ["battery life"] }
            """);

        using var response = await client.PostAsJsonAsync(Url, new { text = "50 bin TL, 16 GB RAM, iyi pil" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var criteria = json.RootElement.GetProperty("criteria");
        Assert.Equal(50000m, criteria.GetProperty("budgetMax").GetDecimal());
        Assert.Equal(16, criteria.GetProperty("minRamGb").GetInt32());
        Assert.Equal(JsonValueKind.Null, criteria.GetProperty("maxWeightKg").ValueKind);
        Assert.Equal(4, criteria.GetProperty("importance").GetProperty("weight").GetInt32());
        Assert.Equal("battery life", json.RootElement.GetProperty("unsupported")[0].GetString());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "importance": { "ram": 9 } }""")]
    [InlineData("""{ "budgetMax": 50000, "winner": 13 }""")]
    public async Task Unusable_model_output_returns_502_problem_details(string output)
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, output);

        using var response = await client.PostAsJsonAsync(Url, new { text = "50 bin TL bütçem var" });

        await AssertProblem(response, HttpStatusCode.BadGateway);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"text":"   "}""")]
    [InlineData("""{"text":5}""")]
    public async Task Invalid_request_returns_400(string body)
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, "{}");
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(Url, content);

        await AssertProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Too_long_text_returns_400()
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, "{}");

        using var response = await client.PostAsJsonAsync(Url, new { text = new string('a', 1001) });

        await AssertProblem(response, HttpStatusCode.BadRequest);
    }

    // output null keeps the configured (unconfigured) extractor. The derived factory is disposed
    // together with the factory the test owns.
    private static HttpClient SignedInClient(AuthTestTokens tokens, PikwiseApiFactory factory, string? output)
    {
        var client = tokens.Configure(factory, services =>
        {
            if (output is not null) services.AddSingleton<IRequirementExtractor>(new FakeExtractor(output));
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create());
        return client;
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
    }

    private sealed class FakeExtractor(string output) : IRequirementExtractor
    {
        public Task<string> ExtractAsync(string text, CancellationToken cancellationToken) => Task.FromResult(output);
    }
}
