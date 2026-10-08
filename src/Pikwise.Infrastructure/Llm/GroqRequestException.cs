namespace Pikwise.Infrastructure.Llm;

// Infrastructure-only failure of a Groq call; adapters translate it to the Application exception
// of their feature. The message holds at most an HTTP status code, never request or response text.
public sealed class GroqRequestException(string message, Exception? innerException = null)
    : Exception(message, innerException);
