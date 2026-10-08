using Pikwise.Application.Explanations.Exceptions;
using Pikwise.Application.Explanations.Interfaces;

namespace Pikwise.Infrastructure.Llm;

// Default until a language model provider is approved and configured (ADR-027).
// Recommendations still work; only the explanation endpoint answers 503.
public sealed class UnconfiguredExplanationGenerator : IExplanationGenerator
{
    public Task<string> GenerateAsync(string inputJson, CancellationToken cancellationToken) =>
        throw new ExplanationUnavailableException("No language model provider is configured.");
}
