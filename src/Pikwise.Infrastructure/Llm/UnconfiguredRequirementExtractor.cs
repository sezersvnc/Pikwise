using Pikwise.Application.RequirementParsing.Exceptions;
using Pikwise.Application.RequirementParsing.Interfaces;

namespace Pikwise.Infrastructure.Llm;

// Default until a language model provider is approved and configured (ADR-026).
// The API still starts; only natural-language parsing answers 503.
public sealed class UnconfiguredRequirementExtractor : IRequirementExtractor
{
    public Task<string> ExtractAsync(string text, CancellationToken cancellationToken) =>
        throw new RequirementExtractionUnavailableException("No language model provider is configured.");
}
