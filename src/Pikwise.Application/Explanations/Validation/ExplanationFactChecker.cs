using System.Globalization;
using System.Text.RegularExpressions;
using Pikwise.Application.Explanations.Exceptions;
using Pikwise.Application.Explanations.Models;

namespace Pikwise.Application.Explanations.Validation;

// Deterministic guard against invented facts. Any failure rejects the whole explanation (ADR-027).
// Limits: numbers written as words ("üç") and invented facts without digits are not detected;
// the instructions forbid them, and the ranking itself never comes from the model.
public static partial class ExplanationFactChecker
{
    public const int MaxTextLength = 1200;

    public static void Check(GeneratedExplanation output, ExplanationInput input, string inputJson)
    {
        var items = output.Products ?? throw Invalid("The products list is missing.");
        var expectedIds = input.Products.Select(product => product.ProductId).ToHashSet();
        var returnedIds = items.Select(item => item?.ProductId).ToList();
        if (returnedIds.Count != expectedIds.Count || returnedIds.Distinct().Count() != returnedIds.Count
            || !returnedIds.All(id => id is not null && expectedIds.Contains(id.Value)))
            throw Invalid("Explanations must cover each recommended product exactly once.");
        if (input.ValueAnalysis is null && output.ValueComment is not null)
            throw Invalid("A value comment was returned without a value analysis.");

        var allowed = AllowedNumbers(inputJson);
        foreach (var item in items)
            CheckText(item!.Explanation, allowed, $"product {item.ProductId}");
        if (output.ValueComment is not null)
            CheckText(output.ValueComment, allowed, "the value comment");
    }

    private static void CheckText(string? text, HashSet<decimal> allowed, string owner)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxTextLength)
            throw Invalid($"The text for {owner} is empty or longer than {MaxTextLength} characters.");
        foreach (Match match in TextNumber().Matches(text))
        {
            if (!Interpretations(match).Any(allowed.Contains))
                throw Invalid($"The text for {owner} contains a number that is not in the input.");
        }
    }

    // The input JSON is invariant: '.' is the decimal separator and numbers have no grouping.
    // Digits inside strings (model names, resolutions) are allowed too. Rounded forms
    // (0, 1 and 2 decimals) are accepted because rounding does not invent a fact.
    private static HashSet<decimal> AllowedNumbers(string inputJson)
    {
        var allowed = new HashSet<decimal>();
        foreach (Match match in InputNumber().Matches(inputJson))
        {
            var value = decimal.Parse(match.Value, CultureInfo.InvariantCulture);
            allowed.Add(value);
            for (var decimals = 0; decimals <= 2; decimals++)
                allowed.Add(Math.Round(value, decimals, MidpointRounding.AwayFromZero));
        }
        return allowed;
    }

    // Turkish text may write 68.000 or 1,5; English style 68,000 or 1.5. Every reading is tried,
    // and "bin" / "milyon" multiply the number. A number is accepted when any reading is in the input.
    private static IEnumerable<decimal> Interpretations(Match match)
    {
        var token = match.Groups["number"].Value;
        var scale = match.Groups["scale"].Value.ToLowerInvariant() switch
        {
            "bin" => 1_000m,
            "milyon" => 1_000_000m,
            _ => 1m
        };
        foreach (var candidate in new[] { token.Replace(".", "").Replace(",", "."), token.Replace(",", "") })
        {
            if (decimal.TryParse(candidate, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
                yield return value * scale;
        }
    }

    private static ExplanationInvalidException Invalid(string reason) => new(reason);

    [GeneratedRegex(@"\d+(?:\.\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex InputNumber();

    [GeneratedRegex(@"(?<number>\d+(?:[.,]\d+)*)(?:\s*(?<scale>bin|milyon)\b)?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex TextNumber();
}
