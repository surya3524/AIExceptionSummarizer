using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErrorSummarizer.Api.Services;

/// <summary>
/// Simple deterministic fake LLM client (replace with real HTTP call to Azure OpenAI / OpenAI etc.)
/// </summary>
public class FakeLlmClient : ILlmClient
{
    public Task<LlmSummaryResult> GetSummaryAsync(string prompt, CancellationToken ct = default)
    {
        // Very naive heuristic parse of exception segment from prompt
        var result = new LlmSummaryResult
        {
            RootCauseHypothesis = InferRootCause(prompt),
            LikelyComponent = InferComponent(prompt),
            ImmediateFix = "Check logs, reproduce locally, add null checks / input validation.",
            LongTermPrevention = "Add automated tests covering this path and strengthen monitoring/alerts.",
            RawModelResponse = "(FAKE MODEL)" + prompt.Take(200)
        };
        return Task.FromResult(result);
    }

    private static string InferRootCause(string prompt)
    {
        if (prompt.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase)) return "Dereferenced a null object (likely missing dependency or bad assumption).";
        if (prompt.Contains("Timeout", StringComparison.OrdinalIgnoreCase)) return "Downstream dependency latency or deadlock.";
        if (prompt.Contains("Sql", StringComparison.OrdinalIgnoreCase)) return "Database query or connection issue.";
        return "Unhandled exception path with insufficient validation.";
    }

    private static string InferComponent(string prompt)
    {
        if (prompt.Contains("Controller", StringComparison.OrdinalIgnoreCase)) return "API Controller Layer";
        if (prompt.Contains("Repository", StringComparison.OrdinalIgnoreCase)) return "Data Access";
        if (prompt.Contains("Service", StringComparison.OrdinalIgnoreCase)) return "Domain Service";
        return "Unknown";
    }
}
