using Microsoft.Extensions.Configuration;

namespace Pikwise.Infrastructure.Llm;

// Groq settings. ApiKey must come from User Secrets or an environment variable (Groq__ApiKey)
// and is never committed or logged. A class, not a record, so ToString cannot print the key.
public sealed class GroqOptions
{
    public const string SectionName = "Groq";
    public static readonly Uri DefaultBaseUri = new("https://api.groq.com/openai/v1/");

    public required string ApiKey { get; init; }
    public Uri BaseUri { get; init; } = DefaultBaseUri;
    // Must support strict json_schema structured outputs (ADR-028).
    public string Model { get; init; } = "openai/gpt-oss-120b";
    public string ReasoningEffort { get; init; } = "low";
    public int MaxCompletionTokens { get; init; } = 2048;

    // Returns null when no key is configured, so the unconfigured implementations stay registered.
    public static GroqOptions? FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var apiKey = section["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        return new GroqOptions
        {
            ApiKey = apiKey.Trim(),
            Model = string.IsNullOrWhiteSpace(section["Model"]) ? "openai/gpt-oss-120b" : section["Model"]!,
            ReasoningEffort = string.IsNullOrWhiteSpace(section["ReasoningEffort"]) ? "low" : section["ReasoningEffort"]!,
            MaxCompletionTokens = int.TryParse(section["MaxCompletionTokens"], out var tokens) && tokens > 0 ? tokens : 2048
        };
    }
}
