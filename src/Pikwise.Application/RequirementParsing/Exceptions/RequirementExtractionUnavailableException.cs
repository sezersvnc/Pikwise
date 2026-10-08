namespace Pikwise.Application.RequirementParsing.Exceptions;

// The language model could not be reached, is not configured or did not answer in time.
public sealed class RequirementExtractionUnavailableException(string reason, Exception? innerException = null)
    : Exception(reason, innerException);
