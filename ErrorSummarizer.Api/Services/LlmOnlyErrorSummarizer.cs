using System.Text;
using Microsoft.Extensions.Options;
using ErrorSummarizer.Api.Options;

namespace ErrorSummarizer.Api.Services;

/// <summary>
/// Summarizer that only uses the LLM client; if disabled or failure, returns a minimal message.
/// </summary>
public sealed class LlmOnlyErrorSummarizer : IErrorSummarizer
{
    private readonly ILlmClient _client;
    private readonly LlmOptions _options;

    public LlmOnlyErrorSummarizer(ILlmClient client, IOptions<LlmOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> SummarizeAsync(Exception ex, ErrorContext context, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return Basic(ex, context, disabled: true);
        }
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
            if (!string.IsNullOrWhiteSpace(result.LikelyComponent)) sb.AppendLine($"LikelyComponent: {result.LikelyComponent}");
            if (!string.IsNullOrWhiteSpace(result.ImmediateFix)) sb.AppendLine($"ImmediateFix: {result.ImmediateFix}");
            if (!string.IsNullOrWhiteSpace(result.LongTermPrevention)) sb.AppendLine($"LongTermPrevention: {result.LongTermPrevention}");
            return sb.ToString();
        }
        catch (Exception e)
        {
            return Basic(ex, context, failure: e.Message);
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

    private static string Basic(Exception ex, ErrorContext ctx, bool disabled = false, string? failure = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Correlation: {ctx.CorrelationId}");
        sb.AppendLine($"Route: {ctx.Method} {ctx.Route}");
        sb.AppendLine($"Type: {ex.GetType().Name}");
        sb.AppendLine($"Message: {ex.Message}");
        if (disabled) sb.AppendLine("LLM disabled.");
        if (failure != null) sb.AppendLine($"LLM failure: {failure}");
        return sb.ToString();
    }
}
