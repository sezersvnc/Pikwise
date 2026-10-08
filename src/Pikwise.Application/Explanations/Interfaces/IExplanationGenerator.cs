namespace Pikwise.Application.Explanations.Interfaces;

// Provider abstraction: sends the serialized ExplanationInput (verified facts and engine output only)
// to a language model and returns its raw JSON. The caller treats the result as untrusted.
// Provider failures surface as ExplanationUnavailableException; no provider-specific type crosses this boundary.
public interface IExplanationGenerator
{
    Task<string> GenerateAsync(string inputJson, CancellationToken cancellationToken);
}
