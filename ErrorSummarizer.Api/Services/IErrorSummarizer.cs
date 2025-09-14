namespace ErrorSummarizer.Api.Services;

public interface IErrorSummarizer
{
    /// <summary>
    /// Produce a concise human friendly summary of an exception.
    /// </summary>
    /// <param name="ex">The exception to summarize.</param>
    /// <param name="correlationId">Correlation id for tracing.</param>
    /// <returns>Summary text.</returns>
    Task<string> SummarizeAsync(Exception ex, string correlationId);
}