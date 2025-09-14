namespace ErrorSummarizer.Api.Services;

public interface ILlmClient
{
    Task<LlmSummaryResult> GetSummaryAsync(string prompt, CancellationToken ct = default);
}
