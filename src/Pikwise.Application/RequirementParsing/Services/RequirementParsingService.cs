using System.Text.Json;
using System.Text.Json.Serialization;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Exceptions;
using Pikwise.Application.Recommendations.Validators;
using Pikwise.Application.RequirementParsing.DTOs;
using Pikwise.Application.RequirementParsing.Exceptions;
using Pikwise.Application.RequirementParsing.Interfaces;
using Pikwise.Application.RequirementParsing.Models;
using Pikwise.Application.RequirementParsing.Validators;

namespace Pikwise.Application.RequirementParsing.Services;

// Turns natural language into validated recommendation criteria. The language model output is
// untrusted: it must match the schema exactly and pass the same rules as a hand-written request.
// Nothing here ranks products; the client sends the criteria to the deterministic engine itself.
public sealed class RequirementParsingService(IRequirementExtractor extractor, TimeSpan? timeout = null)
    : IRequirementParsingService
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    public const int MaxUnsupportedItems = 10;
    public const int MaxUnsupportedLength = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // Unknown fields, duplicate fields and numbers sent as strings are rejected, not ignored.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        NumberHandling = JsonNumberHandling.Strict
    };

    private readonly TimeSpan timeout = timeout ?? DefaultTimeout;

    public async Task<ParsedRequirementsResponseDto> ParseAsync(
        NaturalLanguageRequestDto request, CancellationToken cancellationToken = default)
    {
        NaturalLanguageRequestValidator.Validate(request);
        var text = request.Text!.Trim();
        var extracted = Deserialize(await ExtractAsync(text, cancellationToken));

        var criteria = new RecommendationRequestDto
        {
            BudgetMax = extracted.BudgetMax,
            MinRamGb = extracted.MinRamGb,
            MinStorageGb = extracted.MinStorageGb,
            MaxWeightKg = extracted.MaxWeightKg,
            Importance = extracted.Importance
        };
        try
        {
            RecommendationRequestValidator.Validate(criteria);
        }
        catch (RecommendationValidationException exception)
        {
            throw new RequirementExtractionInvalidException(
                "Extracted criteria are out of range: " + string.Join(", ", exception.Errors.Keys) + ".", exception);
        }
        // A budget is a hard constraint, so it must come from a number the user actually wrote.
        if (criteria.BudgetMax is not null && !text.Any(char.IsAsciiDigit))
            throw new RequirementExtractionInvalidException("A budget was returned although the text contains no digits.");

        return new ParsedRequirementsResponseDto(criteria, NormalizeUnsupported(extracted.Unsupported));
    }

    private async Task<string> ExtractAsync(string text, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            // WaitAsync also stops waiting for a provider that ignores cancellation.
            return await extractor.ExtractAsync(text, timeoutSource.Token).WaitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RequirementExtractionUnavailableException("The language model did not respond in time.", exception);
        }
    }

    private static ExtractedRequirements Deserialize(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            throw new RequirementExtractionInvalidException("The language model returned no output.");
        try
        {
            return JsonSerializer.Deserialize<ExtractedRequirements>(output, JsonOptions)
                ?? throw new RequirementExtractionInvalidException("The language model returned null.");
        }
        catch (JsonException exception)
        {
            throw new RequirementExtractionInvalidException("The language model output does not match the schema.", exception);
        }
    }

    private static IReadOnlyList<string> NormalizeUnsupported(IReadOnlyList<string?>? items)
    {
        var result = (items ?? []).Select(item => item?.Trim() ?? string.Empty).ToList();
        if (result.Count > MaxUnsupportedItems || result.Any(item => item.Length is 0 or > MaxUnsupportedLength))
            throw new RequirementExtractionInvalidException("The unsupported list has empty or too many/too long items.");
        return result;
    }
}
