using System.Text.Json;
using System.Text.Json.Nodes;
using Pikwise.Application.Explanations.Exceptions;
using Pikwise.Application.Explanations.Interfaces;
using Pikwise.Application.Explanations.Models;
using Pikwise.Application.Explanations.Services;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Exceptions;
using Pikwise.Application.Recommendations.Interfaces;
using Pikwise.Application.Recommendations.Services;
using Pikwise.Domain.Entities;

namespace Pikwise.UnitTests;

public class ExplanationServiceTests
{
    // Session 11 sample user: the engine ranks products 13, 3 and 16 on the Session 11.5 dataset.
    private static RecommendationRequestDto SampleRequest() => new()
    {
        BudgetMax = 70000m,
        MinRamGb = 16,
        Importance = new RecommendationImportanceDto { Ram = 3, Storage = 3, Cpu = 4, Gpu = 2, Weight = 5, RefreshRate = 1 }
    };

    [Fact]
    public async Task Explains_the_engine_ranking_in_rank_order_using_only_input_facts()
    {
        var generator = new FakeGenerator(HonestOutput);

        var response = await Service(generator).ExplainAsync(SampleRequest());

        Assert.Equal(new[] { 13, 3, 16 }, response.Recommendation.Items.Select(item => item.ProductId));
        Assert.Equal(new[] { 13, 3, 16 }, response.Explanations.Select(item => item.ProductId));
        Assert.StartsWith("DELL PW514265: 68.000 TL", response.Explanations[0].Explanation);
        Assert.Equal("En iyi seçim 13 numaralı ürün.", response.ValueComment);
        var input = JsonNode.Parse(generator.ReceivedInput!)!;
        Assert.Equal(new[] { 13, 3, 16 }, input["products"]!.AsArray().Select(product => (int)product!["productId"]!));
        Assert.Equal(70000m, (decimal)input["criteria"]!["budgetMax"]!);
        Assert.Null(input["products"]![0]!["components"]![0]!["normalizedValue"]);
    }

    [Fact]
    public async Task Response_contains_the_same_ranking_as_the_recommendation_endpoint()
    {
        var expected = await new RecommendationService(new Repository()).RecommendAsync(SampleRequest());

        var response = await Service(new FakeGenerator(HonestOutput)).ExplainAsync(SampleRequest());

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(response.Recommendation));
    }

    [Fact]
    public async Task Invalid_request_fails_before_the_language_model_is_called()
    {
        var generator = new FakeGenerator(HonestOutput);

        await Assert.ThrowsAsync<RecommendationValidationException>(
            () => Service(generator).ExplainAsync(new RecommendationRequestDto { MinRamGb = 0 }));

        Assert.Null(generator.ReceivedInput);
    }

    [Fact]
    public async Task Empty_ranking_returns_no_explanations_without_calling_the_language_model()
    {
        var generator = new FakeGenerator(HonestOutput);

        var response = await Service(generator).ExplainAsync(new RecommendationRequestDto { BudgetMax = 1m });

        Assert.Empty(response.Recommendation.Items);
        Assert.Empty(response.Explanations);
        Assert.Null(response.ValueComment);
        Assert.Null(generator.ReceivedInput);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{ "products": [], "valueComment": null, "winner": 3 }""")]
    [InlineData("""{ "products": [ { "productId": "13", "explanation": "a" } ], "valueComment": null }""")]
    public async Task Output_that_breaks_the_schema_is_rejected(string output)
    {
        await Assert.ThrowsAsync<ExplanationInvalidException>(
            () => Service(new FakeGenerator(_ => output)).ExplainAsync(SampleRequest()));
    }

    [Fact]
    public async Task Output_with_an_invented_number_is_rejected_as_a_whole()
    {
        var generator = new FakeGenerator(input => HonestOutput(input, " Pil ömrü 9999 dakika."));

        await Assert.ThrowsAsync<ExplanationInvalidException>(() => Service(generator).ExplainAsync(SampleRequest()));
    }

    [Fact]
    public async Task Provider_that_honours_cancellation_times_out_as_unavailable()
    {
        var generator = new FakeGenerator(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return "{}";
        });

        await Assert.ThrowsAsync<ExplanationUnavailableException>(
            () => Service(generator, TimeSpan.FromMilliseconds(50)).ExplainAsync(SampleRequest()));
    }

    [Fact]
    public async Task Provider_that_ignores_cancellation_still_times_out_as_unavailable()
    {
        var never = new TaskCompletionSource<string>();

        await Assert.ThrowsAsync<ExplanationUnavailableException>(
            () => Service(new FakeGenerator((_, _) => never.Task), TimeSpan.FromMilliseconds(50)).ExplainAsync(SampleRequest()));
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_a_provider_failure()
    {
        using var cancellation = new CancellationTokenSource();
        var generator = new FakeGenerator(async (_, token) =>
        {
            await cancellation.CancelAsync();
            await Task.Delay(Timeout.Infinite, token);
            return "{}";
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Service(generator).ExplainAsync(SampleRequest(), cancellation.Token));
    }

    [Fact]
    public async Task Provider_failure_is_passed_on_as_unavailable()
    {
        var generator = new FakeGenerator((_, _) => throw new ExplanationUnavailableException("down"));

        await Assert.ThrowsAsync<ExplanationUnavailableException>(() => Service(generator).ExplainAsync(SampleRequest()));
    }

    [Fact]
    public void Json_schema_lists_exactly_the_fields_the_parser_accepts()
    {
        using var schema = JsonDocument.Parse(ExplanationContract.JsonSchema);
        var root = schema.RootElement;
        var item = root.GetProperty("properties").GetProperty("products").GetProperty("items");

        Assert.Equal(CamelCaseNames<GeneratedExplanation>(), Names(root.GetProperty("properties")));
        Assert.Equal(CamelCaseNames<GeneratedExplanation>(), Names(root.GetProperty("required")));
        Assert.Equal(CamelCaseNames<GeneratedProductExplanation>(), Names(item.GetProperty("properties")));
        Assert.Equal(CamelCaseNames<GeneratedProductExplanation>(), Names(item.GetProperty("required")));
        Assert.False(root.GetProperty("additionalProperties").GetBoolean());
        Assert.False(item.GetProperty("additionalProperties").GetBoolean());
    }

    // A well-behaved model: it repeats names, prices (Turkish grouping) and scores from the input only.
    private static string HonestOutput(string inputJson) => HonestOutput(inputJson, string.Empty);

    private static string HonestOutput(string inputJson, string valueCommentSuffix)
    {
        var input = JsonNode.Parse(inputJson)!;
        var products = input["products"]!.AsArray().Select(product => new
        {
            productId = (int)product!["productId"]!,
            explanation = $"{product["name"]}: {(decimal)product["price"]!:#,0} TL, {(decimal)product["score"]!} puan."
                .Replace(",", ".")
        });
        var bestFit = (int)input["valueAnalysis"]!["bestFitProductId"]!;
        return JsonSerializer.Serialize(new { products, valueComment = $"En iyi seçim {bestFit} numaralı ürün." + valueCommentSuffix });
    }

    private static ExplanationService Service(IExplanationGenerator generator, TimeSpan? timeout = null) =>
        new(new RecommendationService(new Repository()), generator, timeout);

    private static string[] Names(JsonElement element) => (element.ValueKind == JsonValueKind.Array
        ? element.EnumerateArray().Select(item => item.GetString()!)
        : element.EnumerateObject().Select(property => property.Name)).Order().ToArray();

    private static string[] CamelCaseNames<T>() => typeof(T).GetProperties()
        .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name)).Order().ToArray();

    private sealed class Repository : IRecommendationRepository
    {
        public Task<IReadOnlyList<Product>> GetCandidatesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(RecommendationTestData.Session115Laptops());
    }

    private sealed class FakeGenerator(Func<string, CancellationToken, Task<string>> answer) : IExplanationGenerator
    {
        public FakeGenerator(Func<string, string> answer) : this((input, _) => Task.FromResult(answer(input))) { }

        public string? ReceivedInput { get; private set; }

        public Task<string> GenerateAsync(string inputJson, CancellationToken cancellationToken)
        {
            ReceivedInput = inputJson;
            return answer(inputJson, cancellationToken);
        }
    }
}
