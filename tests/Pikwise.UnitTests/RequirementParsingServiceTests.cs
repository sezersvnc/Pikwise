using System.Text.Json;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Exceptions;
using Pikwise.Application.RequirementParsing.DTOs;
using Pikwise.Application.RequirementParsing.Exceptions;
using Pikwise.Application.RequirementParsing.Interfaces;
using Pikwise.Application.RequirementParsing.Models;
using Pikwise.Application.RequirementParsing.Services;

namespace Pikwise.UnitTests;

public class RequirementParsingServiceTests
{
    private const string SampleText = "50 bin TL bütçem var, okul ve yazılım için kullanacağım, arada oyun oynarım, çok ağır olmasın.";

    // What a well-behaved model is expected to return for SampleText.
    private const string SampleOutput = """
        { "budgetMax": 50000, "minRamGb": null, "minStorageGb": null, "maxWeightKg": null,
          "importance": { "ram": 4, "storage": null, "cpu": 4, "gpu": 3, "weight": 4, "refreshRate": 3 },
          "unsupported": [] }
        """;

    [Fact]
    public async Task Valid_output_becomes_recommendation_criteria_and_only_trimmed_user_text_is_sent()
    {
        var extractor = new FakeExtractor(SampleOutput);

        var result = await Service(extractor).ParseAsync(Request("  " + SampleText + "  "));

        Assert.Equal(SampleText, extractor.ReceivedText);
        Assert.Equal(50000m, result.Criteria.BudgetMax);
        Assert.Null(result.Criteria.MinRamGb);
        Assert.Null(result.Criteria.MinStorageGb);
        Assert.Null(result.Criteria.MaxWeightKg);
        var importance = result.Criteria.Importance!;
        Assert.Equal(new int?[] { 4, null, 4, 3, 4, 3 },
            new[] { importance.Ram, importance.Storage, importance.Cpu, importance.Gpu, importance.Weight, importance.RefreshRate });
        Assert.Empty(result.Unsupported);
    }

    [Fact]
    public async Task Missing_fields_stay_null_and_unsupported_wishes_are_trimmed()
    {
        var output = """{ "importance": null, "unsupported": [" battery life ", "OLED screen"] }""";

        var result = await Service(new FakeExtractor(output)).ParseAsync(Request("uzun pil ömrü ve OLED ekran"));

        Assert.Null(result.Criteria.BudgetMax);
        Assert.Null(result.Criteria.Importance);
        Assert.Equal(new[] { "battery life", "OLED screen" }, result.Unsupported);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{ "budgetMax": 50000""")]
    [InlineData("""{ "winner": 13 }""")]
    [InlineData("""{ "importance": { "ram": 4, "battery": 5 } }""")]
    [InlineData("""{ "budgetMax": "50000" }""")]
    [InlineData("""{ "budgetMax": 1, "budgetMax": 50000 }""")]
    [InlineData("""{ "minRamGb": 16.5 }""")]
    [InlineData("""{ "minRamGb": 0 }""")]
    [InlineData("""{ "maxWeightKg": 0.05 }""")]
    [InlineData("""{ "budgetMax": -1 }""")]
    [InlineData("""{ "budgetMax": 50000.001 }""")]
    [InlineData("""{ "importance": { "gpu": 6 } }""")]
    [InlineData("""{ "importance": { "weight": 0 } }""")]
    [InlineData("""{ "unsupported": [""] }""")]
    [InlineData("""{ "unsupported": [null] }""")]
    [InlineData("""{ "unsupported": ["1","2","3","4","5","6","7","8","9","10","11"] }""")]
    public async Task Untrusted_output_that_breaks_the_schema_or_the_rules_is_rejected(string output)
    {
        await Assert.ThrowsAsync<RequirementExtractionInvalidException>(
            () => Service(new FakeExtractor(output)).ParseAsync(Request("16 GB RAM, 50000 TL")));
    }

    [Fact]
    public async Task Too_long_unsupported_item_is_rejected()
    {
        var output = JsonSerializer.Serialize(new { unsupported = new[] { new string('x', 101) } });

        await Assert.ThrowsAsync<RequirementExtractionInvalidException>(
            () => Service(new FakeExtractor(output)).ParseAsync(Request("metin")));
    }

    [Fact]
    public async Task Budget_without_digits_in_the_text_is_rejected_as_invented()
    {
        await Assert.ThrowsAsync<RequirementExtractionInvalidException>(
            () => Service(new FakeExtractor("""{ "budgetMax": 50000 }""")).ParseAsync(Request("elli bin lira bütçem var")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_text_is_a_validation_error_and_the_model_is_not_called(string? text)
    {
        var extractor = new FakeExtractor(SampleOutput);

        var exception = await Assert.ThrowsAsync<RecommendationValidationException>(
            () => Service(extractor).ParseAsync(Request(text)));

        Assert.Contains(nameof(NaturalLanguageRequestDto.Text), exception.Errors.Keys);
        Assert.Null(extractor.ReceivedText);
    }

    [Fact]
    public async Task Too_long_text_is_a_validation_error()
    {
        var text = new string('a', NaturalLanguageRequestDto.MaxTextLength + 1);

        await Assert.ThrowsAsync<RecommendationValidationException>(
            () => Service(new FakeExtractor(SampleOutput)).ParseAsync(Request(text)));
    }

    [Fact]
    public async Task Provider_that_honours_cancellation_times_out_as_unavailable()
    {
        var extractor = new FakeExtractor(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return SampleOutput;
        });

        await Assert.ThrowsAsync<RequirementExtractionUnavailableException>(
            () => Service(extractor, TimeSpan.FromMilliseconds(50)).ParseAsync(Request(SampleText)));
    }

    [Fact]
    public async Task Provider_that_ignores_cancellation_still_times_out_as_unavailable()
    {
        var never = new TaskCompletionSource<string>();

        await Assert.ThrowsAsync<RequirementExtractionUnavailableException>(
            () => Service(new FakeExtractor(_ => never.Task), TimeSpan.FromMilliseconds(50)).ParseAsync(Request(SampleText)));
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_a_provider_failure()
    {
        using var cancellation = new CancellationTokenSource();
        var extractor = new FakeExtractor(async token =>
        {
            await cancellation.CancelAsync();
            await Task.Delay(Timeout.Infinite, token);
            return SampleOutput;
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Service(extractor).ParseAsync(Request(SampleText), cancellation.Token));
    }

    [Fact]
    public async Task Provider_failure_is_passed_on_as_unavailable()
    {
        var extractor = new FakeExtractor(_ => throw new RequirementExtractionUnavailableException("down"));

        await Assert.ThrowsAsync<RequirementExtractionUnavailableException>(
            () => Service(extractor).ParseAsync(Request(SampleText)));
    }

    [Fact]
    public void Json_schema_lists_exactly_the_fields_the_parser_accepts()
    {
        using var schema = JsonDocument.Parse(RequirementExtractionContract.JsonSchema);
        var root = schema.RootElement;
        var importance = root.GetProperty("properties").GetProperty("importance");

        Assert.Equal(CamelCaseNames<ExtractedRequirements>(), Names(root.GetProperty("properties")));
        Assert.Equal(CamelCaseNames<ExtractedRequirements>(), Names(root.GetProperty("required")));
        Assert.Equal(CamelCaseNames<RecommendationImportanceDto>(), Names(importance.GetProperty("properties")));
        Assert.Equal(CamelCaseNames<RecommendationImportanceDto>(), Names(importance.GetProperty("required")));
        Assert.False(root.GetProperty("additionalProperties").GetBoolean());
        Assert.False(importance.GetProperty("additionalProperties").GetBoolean());
    }

    private static RequirementParsingService Service(IRequirementExtractor extractor, TimeSpan? timeout = null) =>
        new(extractor, timeout);

    private static NaturalLanguageRequestDto Request(string? text) => new() { Text = text };

    private static string[] Names(JsonElement element) => (element.ValueKind == JsonValueKind.Array
        ? element.EnumerateArray().Select(item => item.GetString()!)
        : element.EnumerateObject().Select(property => property.Name)).Order().ToArray();

    private static string[] CamelCaseNames<T>() => typeof(T).GetProperties()
        .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name)).Order().ToArray();

    private sealed class FakeExtractor(Func<CancellationToken, Task<string>> answer) : IRequirementExtractor
    {
        public FakeExtractor(string output) : this(_ => Task.FromResult(output)) { }

        public string? ReceivedText { get; private set; }

        public Task<string> ExtractAsync(string text, CancellationToken cancellationToken)
        {
            ReceivedText = text;
            return answer(cancellationToken);
        }
    }
}
