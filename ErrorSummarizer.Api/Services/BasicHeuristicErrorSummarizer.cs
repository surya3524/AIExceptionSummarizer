using System.Text;

namespace ErrorSummarizer.Api.Services;

/// <summary>
/// Lightweight heuristic summarizer (placeholder for real AI model / LLM call).
/// Keeps everything in-process and deterministic for now.
/// </summary>
public class BasicHeuristicErrorSummarizer : IErrorSummarizer
{
    public Task<string> SummarizeAsync(Exception ex, string correlationId)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Correlation: {correlationId}");
        sb.AppendLine($"Type: {ex.GetType().Name}");
        sb.AppendLine($"Message: {ex.Message}");
        if (ex.InnerException != null)
        {
            sb.AppendLine($"Inner: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
        }
        // Provide probable causes heuristically
        var hint = InferHint(ex);
        if (!string.IsNullOrWhiteSpace(hint))
        {
            sb.AppendLine($"Possible Cause: {hint}");
        }
        // Provide first stack frame only (most relevant origin)
        var firstLine = ex.StackTrace?.Split('\n').FirstOrDefault(l => l.Contains(" at "))?.Trim();
        if (!string.IsNullOrWhiteSpace(firstLine))
        {
            sb.AppendLine($"Origin: {firstLine}");
        }
        return Task.FromResult(sb.ToString());
    }

    private string InferHint(Exception ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        if (msg.Contains("null")) return "A null reference occurred. Check for missing dependency or uninitialized variable.";
        if (msg.Contains("timeout")) return "An operation exceeded the allowed time. Investigate external service / DB latency.";
        if (msg.Contains("not found")) return "A resource was not found. Validate identifiers or existence of resource.";
        if (msg.Contains("unauthorized") || msg.Contains("forbidden")) return "Authentication/authorization issue. Check credentials and permissions.";
        if (ex is InvalidOperationException) return "Sequence of calls or object state invalid. Review lifecycle and state transitions.";
        if (ex is ArgumentException) return "Input parameter invalid. Validate arguments before invoking API.";
        return string.Empty;
    }
}