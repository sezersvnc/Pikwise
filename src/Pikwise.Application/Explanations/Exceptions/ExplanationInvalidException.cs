namespace Pikwise.Application.Explanations.Exceptions;

// The language model answered, but its output failed the schema or the fact check.
// The reason names the failed rule only; it never contains product text or user data.
public sealed class ExplanationInvalidException(string reason, Exception? innerException = null)
    : Exception(reason, innerException);
