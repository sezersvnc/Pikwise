namespace Pikwise.Application.Explanations.Exceptions;

// The language model could not be reached, is not configured or did not answer in time.
public sealed class ExplanationUnavailableException(string reason, Exception? innerException = null)
    : Exception(reason, innerException);
