using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Pikwise.Application.Explanations.Models;

// Provider-neutral structured-output contract. A provider adapter sends Instructions as the system
// message, SerializeInput's JSON as the only user message and JsonSchema as the output format.
// Icecat-sourced facts are request-time input only: no storage, training or embeddings (ADR-025).
public static class ExplanationContract
{
    public const string Instructions = """
        You explain the result of a deterministic laptop recommendation engine to a Turkish shopper.
        Write in Turkish. Return only a JSON object that matches the given schema.

        Use only the data in the input JSON:
        - The ranking is final. Never change it, never call another product the winner and never
          mention products that are not in the input.
        - Use only numbers that appear in the input, copied exactly (rounding is allowed).
          Do not calculate new numbers such as percentages, differences or ratios.
        - A null specification field or a criterion in unknownCriteria is unknown: say "bilgi yok";
          never guess it.
        - Prices are development demo prices, not market prices. Do not call them current or market prices.
        - Do not add facts such as battery life, benchmarks, stock, reviews or release dates.

        products: one entry per input product, with its productId, in ranking order. Each explanation is at
        most three plain sentences (no markdown, no lists) on why the product fits the user's criteria,
        based on its score components.
        valueComment: when valueAnalysis is present, two or three sentences on whether paying more for the
        best fit is worth it, using only valueAnalysis. Null when valueAnalysis is null.
        """;

    public const string JsonSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["products", "valueComment"],
          "properties": {
            "products": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["productId", "explanation"],
                "properties": {
                  "productId": { "type": "integer" },
                  "explanation": { "type": "string" }
                }
              }
            },
            "valueComment": { "type": ["string", "null"] }
          }
        }
        """;

    // Unicode letters stay readable (not \u escaped) so names reach the model as stored.
    private static readonly JsonSerializerOptions InputOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static string SerializeInput(ExplanationInput input) => JsonSerializer.Serialize(input, InputOptions);
}
