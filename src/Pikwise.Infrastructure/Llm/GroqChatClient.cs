using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Pikwise.Infrastructure.Llm;

// Typed HttpClient for Groq's OpenAI-compatible chat completions endpoint with strict
// json_schema structured output. Base address and the Bearer key are set at registration.
// Request and response text are never logged; only status codes are.
public sealed class GroqChatClient(HttpClient httpClient, GroqOptions options, ILogger<GroqChatClient> logger)
{
    public async Task<string> CompleteAsync(
        string instructions, string userContent, string schemaName, string jsonSchema, CancellationToken cancellationToken)
    {
        using var schema = JsonDocument.Parse(jsonSchema);
        var body = new
        {
            model = options.Model,
            messages = new[]
            {
                new { role = "system", content = instructions },
                new { role = "user", content = userContent }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new { name = schemaName, strict = true, schema = schema.RootElement }
            },
            reasoning_effort = options.ReasoningEffort,
            include_reasoning = false,
            max_completion_tokens = options.MaxCompletionTokens
        };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("chat/completions", body, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning("Groq could not be reached.");
            throw new GroqRequestException("The language model could not be reached.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // 429 (free-tier limit), 401 (wrong key) and 5xx all mean the model is unavailable for now.
                logger.LogWarning("Groq answered with status {StatusCode}.", (int)response.StatusCode);
                throw new GroqRequestException($"The language model answered with status {(int)response.StatusCode}.");
            }
            try
            {
                var completion = await response.Content.ReadFromJsonAsync<ChatCompletion>(cancellationToken);
                // An empty result is rejected later as unusable output (502) by the Application service.
                return completion?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
            }
            catch (JsonException exception)
            {
                logger.LogWarning("Groq returned a response that is not a chat completion.");
                throw new GroqRequestException("The language model returned an unreadable response.", exception);
            }
        }
    }

    private sealed record ChatCompletion(IReadOnlyList<Choice>? Choices);
    private sealed record Choice(ChatMessage? Message);
    private sealed record ChatMessage(string? Content);
}
