namespace Pikwise.Application.RequirementParsing.Interfaces;

// Provider abstraction: sends only the user's text to a language model and returns its raw JSON.
// The caller treats the result as untrusted. Provider failures surface as
// RequirementExtractionUnavailableException; no provider-specific type crosses this boundary.
public interface IRequirementExtractor
{
    Task<string> ExtractAsync(string text, CancellationToken cancellationToken);
}
