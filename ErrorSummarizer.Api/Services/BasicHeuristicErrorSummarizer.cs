using System.Text;

namespace ErrorSummarizer.Api.Services;

/// <summary>
/// Lightweight heuristic summarizer (placeholder for real AI model / LLM call).
/// Keeps everything in-process and deterministic for now.
/// </summary>
public class BasicHeuristicErrorSummarizer : IErrorSummarizer
{
    public Task<string> SummarizeAsync(Exception ex, ErrorContext context, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Correlation: {context.CorrelationId}");
        sb.AppendLine($"Route: {context.Method} {context.Route}");
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
        // Include top stack frame(s)
        if (!string.IsNullOrWhiteSpace(context.TopStackFrame))
        {
            sb.AppendLine($"Origin: {context.TopStackFrame}");
        }
        if (context.TopAppFrames != null && context.TopAppFrames.Count > 1)
        {
            sb.AppendLine("Frames:");
            foreach (var f in context.TopAppFrames.Skip(1))
            {
                sb.AppendLine($"  {f}");
            }
        }
        if (context.Headers != null)
        {
            sb.AppendLine("Headers:");
            foreach (var kv in context.Headers)
                sb.AppendLine($"  {kv.Key}: {kv.Value}");
        }
        if (context.Claims != null && context.Claims.Count > 0)
        {
            sb.AppendLine("Claims:");
            foreach (var kv in context.Claims)
                sb.AppendLine($"  {kv.Key}: {kv.Value}");
        }
        if (!string.IsNullOrEmpty(context.SanitizedBody))
        {
            sb.AppendLine("Body:");
            sb.AppendLine(context.SanitizedBody);
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