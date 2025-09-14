using System.Text;
using Microsoft.Extensions.Options;

namespace ErrorSummarizer.Api.Services;

/// <summary>
/// LLM-backed summarizer (wraps an ILlmClient). Falls back to heuristic if disabled.
/// </summary>
public class LlmErrorSummarizer : IErrorSummarizer
{
    private readonly ILlmClient _client;
    private readonly IErrorSummarizer _fallback;
    private readonly LlmOptions _options;

    public LlmErrorSummarizer(ILlmClient client, IOptions<LlmOptions> options, BasicHeuristicErrorSummarizer fallback)
    {
        _client = client;
        _fallback = fallback;
        _options = options.Value;
    }

    public async Task<string> SummarizeAsync(Exception ex, ErrorContext context, CancellationToken ct = default)
    {
        if (!_options.Enabled) return await _fallback.SummarizeAsync(ex, context, ct);

        try
        {
            var prompt = BuildPrompt(ex, context);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));
            var result = await _client.GetSummaryAsync(prompt, cts.Token);
            var sb = new StringBuilder();
            sb.AppendLine($"Correlation: {context.CorrelationId}");
            sb.AppendLine($"Route: {context.Method} {context.Route}");
            sb.AppendLine($"Type: {ex.GetType().Name}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine("--- AI Analysis ---");
            sb.AppendLine($"RootCauseHypothesis: {result.RootCauseHypothesis}");
            sb.AppendLine($"LikelyComponent: {result.LikelyComponent}");
            sb.AppendLine($"ImmediateFix: {result.ImmediateFix}");
            sb.AppendLine($"LongTermPrevention: {result.LongTermPrevention}");
            return sb.ToString();
        }
        catch (Exception)
        {
            return await _fallback.SummarizeAsync(ex, context, ct);
        }
    }

    private static string BuildPrompt(Exception ex, ErrorContext ctx)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert software incident responder. Provide concise structured analysis.");
        sb.AppendLine("Return JSON with: rootCauseHypothesis, likelyComponent, immediateFix, longTermPrevention.");
        sb.AppendLine();
        sb.AppendLine("Exception:");
        sb.AppendLine(ex.ToString());
        sb.AppendLine();
        sb.AppendLine("Context:");
        sb.AppendLine($"CorrelationId: {ctx.CorrelationId}");
        sb.AppendLine($"Route: {ctx.Method} {ctx.Route}");
        if (ctx.TopStackFrame != null) sb.AppendLine($"TopFrame: {ctx.TopStackFrame}");
        if (ctx.TopAppFrames != null) foreach (var f in ctx.TopAppFrames) sb.AppendLine($"AppFrame: {f}");
        if (ctx.Headers != null) foreach (var kv in ctx.Headers) sb.AppendLine($"Header: {kv.Key}={kv.Value}");
        if (ctx.Claims != null) foreach (var kv in ctx.Claims) sb.AppendLine($"Claim: {kv.Key}={kv.Value}");
        if (!string.IsNullOrEmpty(ctx.SanitizedBody)) sb.AppendLine($"Body: {ctx.SanitizedBody}");
        return sb.ToString();
    }
}
