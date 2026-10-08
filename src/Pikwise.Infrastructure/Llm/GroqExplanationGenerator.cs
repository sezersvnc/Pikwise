using Pikwise.Application.Explanations.Exceptions;
using Pikwise.Application.Explanations.Interfaces;
using Pikwise.Application.Explanations.Models;

namespace Pikwise.Infrastructure.Llm;

// Sends Icecat-sourced facts as request-time input only; Groq Zero Data Retention is enabled
// for the organization and Groq does not train on API data (ADR-025, ADR-028).
public sealed class GroqExplanationGenerator(GroqChatClient client) : IExplanationGenerator
{
    public async Task<string> GenerateAsync(string inputJson, CancellationToken cancellationToken)
    {
        try
        {
            return await client.CompleteAsync(ExplanationContract.Instructions, inputJson,
                "recommendation_explanation", ExplanationContract.JsonSchema, cancellationToken);
        }
        catch (GroqRequestException exception)
        {
            throw new ExplanationUnavailableException(exception.Message, exception);
        }
    }
}
