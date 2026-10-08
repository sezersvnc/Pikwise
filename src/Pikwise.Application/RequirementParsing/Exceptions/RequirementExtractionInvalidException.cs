namespace Pikwise.Application.RequirementParsing.Exceptions;

// The language model answered, but its output failed the schema or business validation.
// The reason names the failed rule only; it never contains the user's text.
public sealed class RequirementExtractionInvalidException(string reason, Exception? innerException = null)
    : Exception(reason, innerException);
