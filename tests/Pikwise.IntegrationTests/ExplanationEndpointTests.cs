using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Application.Explanations.Interfaces;
using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Interfaces;

namespace Pikwise.IntegrationTests;

// No SQL Server and no real language model: the ranking and the generator are fakes where needed.
public class ExplanationEndpointTests
{
    private const string Url = "/api/recommendations/explanation";

    [Theory]
    [InlineData("""{"minRamGb":0}""")]
    [InlineData("""{"importance":{"ram":9}}""")]
    [InlineData("""{"budgetMax":"cheap"}""")]
    public async Task Invalid_criteria_return_400_without_database_access(string body)
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, generatorOutput: null, fakeRanking: false);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(Url, content);

        await AssertProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Without_a_configured_provider_returns_503_problem_details()
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, generatorOutput: null);

        using var response = await client.PostAsJsonAsync(Url, new { budgetMax = 70000 });

        await AssertProblem(response, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Valid_explanation_is_returned_with_the_ranking_it_explains()
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, """
            { "products": [ { "productId": 13, "explanation": "DELL PW514265 68.000 TL ile bütçeye uyuyor ve 75,41 puan aldı." } ],
              "valueComment": "Daha ucuz ve 3 puandan az geride bir seçenek yok." }
            """);

        using var response = await client.PostAsJsonAsync(Url, new { budgetMax = 70000 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(13, root.GetProperty("recommendation").GetProperty("items")[0].GetProperty("productId").GetInt32());
        Assert.Equal(13, root.GetProperty("explanations")[0].GetProperty("productId").GetInt32());
        Assert.StartsWith("DELL PW514265", root.GetProperty("explanations")[0].GetProperty("explanation").GetString());
        Assert.StartsWith("Daha ucuz", root.GetProperty("valueComment").GetString());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "products": [ { "productId": 13, "explanation": "Pil ömrü 9999 dakika." } ], "valueComment": null }""")]
    [InlineData("""{ "products": [ { "productId": 99, "explanation": "Uygun." } ], "valueComment": null }""")]
    public async Task Unusable_or_unsupported_explanation_returns_502_problem_details(string output)
    {
        using var tokens = new AuthTestTokens();
        await using var factory = new PikwiseApiFactory();
        using var client = SignedInClient(tokens, factory, output);

        using var response = await client.PostAsJsonAsync(Url, new { budgetMax = 70000 });

        await AssertProblem(response, HttpStatusCode.BadGateway);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    public async Task Requires_a_valid_user_token(string token)
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = tokens.Configure(baseFactory);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = JsonContent.Create(new { budgetMax = 70000 }) };
        if (token != "missing") request.Headers.Authorization = new("Bearer", tokens.Create(token));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // generatorOutput null keeps the configured (unconfigured) generator; fakeRanking false keeps the
    // real recommendation service. The derived factory is disposed with the factory the test owns.
    private static HttpClient SignedInClient(
        AuthTestTokens tokens, PikwiseApiFactory factory, string? generatorOutput, bool fakeRanking = true)
    {
        var client = tokens.Configure(factory, services =>
        {
            if (fakeRanking) services.AddScoped<IRecommendationService, FakeRecommendationService>();
            if (generatorOutput is not null)
                services.AddSingleton<IExplanationGenerator>(new FakeGenerator(generatorOutput));
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

    private sealed class FakeRecommendationService : IRecommendationService
    {
        public Task<RecommendationResponseDto> RecommendAsync(RecommendationRequestDto request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RecommendationResponseDto(
                [
                    new RecommendedProductDto(1, 13, "DELL PW514265", 68000.00m, new ProductBrandDto(3, "DELL"),
                        new LaptopSpecificationDto("AMD Ryzen AI 7 PRO 450", null, 32, 512, 14m, null, null, 1.4m, null),
                        75.41m, 18, 18, 1m, [], [new ScoreComponentDto("ram", 32m, 1m, 0.1667m, 16.6667m)])
                ],
                new RecommendationSummaryDto(25, 2, 6, 0, 17),
                new ValueAnalysisDto(13, 13, false, 3m, [])));
    }

    private sealed class FakeGenerator(string output) : IExplanationGenerator
    {
        public Task<string> GenerateAsync(string inputJson, CancellationToken cancellationToken) => Task.FromResult(output);
    }
}
