using System.Text.Json;
using System.Text.Json.Serialization;
using Pikwise.Application.Explanations.DTOs;
using Pikwise.Application.Explanations.Exceptions;
using Pikwise.Application.Explanations.Interfaces;
using Pikwise.Application.Explanations.Mappers;
using Pikwise.Application.Explanations.Models;
using Pikwise.Application.Explanations.Validation;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Interfaces;

namespace Pikwise.Application.Explanations.Services;

// Ranks first, then explains. The deterministic engine decides; the language model only describes
// that decision, and its output is untrusted until the schema and fact checks pass.
public sealed class ExplanationService(
    IRecommendationService recommendationService,
    IExplanationGenerator generator,
    TimeSpan? timeout = null) : IExplanationService
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private static readonly JsonSerializerOptions OutputOptions = new(JsonSerializerDefaults.Web)
    {
        // Unknown fields, duplicate fields and numbers sent as strings are rejected, not ignored.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        NumberHandling = JsonNumberHandling.Strict
    };

    private readonly TimeSpan timeout = timeout ?? DefaultTimeout;

    public async Task<ExplanationResponseDto> ExplainAsync(
        RecommendationRequestDto request, CancellationToken cancellationToken = default)
    {
        // The recommendation service validates the request (400) and produces the final ranking.
        var recommendation = await recommendationService.RecommendAsync(request, cancellationToken);
        if (recommendation.Items.Count == 0)
            return new ExplanationResponseDto(recommendation, [], null);

        var input = recommendation.ToExplanationInput(request);
        var inputJson = ExplanationContract.SerializeInput(input);
        var output = Deserialize(await GenerateAsync(inputJson, cancellationToken));
        ExplanationFactChecker.Check(output, input, inputJson);

        var explanations = output.Products!.ToDictionary(item => item!.ProductId, item => item!.Explanation!.Trim());
        return new ExplanationResponseDto(
            recommendation,
            recommendation.Items.Select(item => new ProductExplanationDto(item.ProductId, explanations[item.ProductId])).ToList(),
            output.ValueComment?.Trim());
    }

    private async Task<string> GenerateAsync(string inputJson, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            // WaitAsync also stops waiting for a provider that ignores cancellation.
            return await generator.GenerateAsync(inputJson, timeoutSource.Token).WaitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExplanationUnavailableException("The language model did not respond in time.", exception);
        }
    }

    private static GeneratedExplanation Deserialize(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            throw new ExplanationInvalidException("The language model returned no output.");
        try
        {
            return JsonSerializer.Deserialize<GeneratedExplanation>(output, OutputOptions)
                ?? throw new ExplanationInvalidException("The language model returned null.");
        }
        catch (JsonException exception)
        {
            throw new ExplanationInvalidException("The language model output does not match the schema.", exception);
        }
    }
}
