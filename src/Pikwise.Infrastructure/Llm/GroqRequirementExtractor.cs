using Pikwise.Application.RequirementParsing.Exceptions;
using Pikwise.Application.RequirementParsing.Interfaces;
using Pikwise.Application.RequirementParsing.Models;

namespace Pikwise.Infrastructure.Llm;

// Sends only the user's text with the provider-neutral contract from Application (ADR-026/028).
public sealed class GroqRequirementExtractor(GroqChatClient client) : IRequirementExtractor
{
    public async Task<string> ExtractAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            return await client.CompleteAsync(RequirementExtractionContract.Instructions, text,
                "recommendation_criteria", RequirementExtractionContract.JsonSchema, cancellationToken);
        }
        catch (GroqRequestException exception)
        {
            throw new RequirementExtractionUnavailableException(exception.Message, exception);
        }
    }
}
