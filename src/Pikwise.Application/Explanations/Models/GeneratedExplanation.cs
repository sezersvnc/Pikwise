namespace Pikwise.Application.Explanations.Models;

// Deserialization target of the language model output; property names match ExplanationContract.JsonSchema.
public sealed class GeneratedExplanation
{
    public IReadOnlyList<GeneratedProductExplanation?>? Products { get; init; }
    public string? ValueComment { get; init; }
}

public sealed class GeneratedProductExplanation
{
    public int ProductId { get; init; }
    public string? Explanation { get; init; }
}
