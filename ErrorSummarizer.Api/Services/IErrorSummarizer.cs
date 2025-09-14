namespace ErrorSummarizer.Api.Services;

public interface IErrorSummarizer
{
    Task<string> SummarizeAsync(Exception ex, ErrorContext context, CancellationToken ct = default);
}