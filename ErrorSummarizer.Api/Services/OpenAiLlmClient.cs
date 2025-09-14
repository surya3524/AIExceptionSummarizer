using System.Net.Http.Json;
using System.Text.Json;
using ErrorSummarizer.Api.Options;
using Microsoft.Extensions.Options;

namespace ErrorSummarizer.Api.Services;

/// <summary>
/// HTTP client implementation that calls OpenAI-compatible chat/completions endpoint.
/// </summary>
public sealed class OpenAiLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly LlmOptions _opts;

    public OpenAiLlmClient(HttpClient http, IOptions<LlmOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
        if (!string.IsNullOrWhiteSpace(_opts.Endpoint))
        {
            _http.BaseAddress ??= new Uri(_opts.Endpoint.TrimEnd('/') + "/");
        }
        if (!_http.DefaultRequestHeaders.Contains("Authorization") && !string.IsNullOrWhiteSpace(_opts.ApiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _opts.ApiKey);
        }
        _http.Timeout = TimeSpan.FromSeconds(Math.Max(1, _opts.TimeoutSeconds));
    }

    public async Task<LlmSummaryResult> GetSummaryAsync(string prompt, CancellationToken ct = default)
    {
        var systemPrompt = "You are an assistant that produces concise JSON root cause analysis for software errors.";
        var request = new
        {
            model = _opts.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            max_tokens = 500
        };

        using var resp = await _http.PostAsJsonAsync("chat/completions", request, ct);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

        // Try parse JSON fields from model response (fallback to raw content)
        string? rootCause = null, component = null, immediate = null, longTerm = null;
        try
        {
            using var inner = JsonDocument.Parse(content);
            var e = inner.RootElement;
            rootCause = e.TryGetProperty("rootCauseHypothesis", out var rc) ? rc.GetString() : null;
            component = e.TryGetProperty("likelyComponent", out var lc) ? lc.GetString() : null;
            immediate = e.TryGetProperty("immediateFix", out var im) ? im.GetString() : null;
            longTerm = e.TryGetProperty("longTermPrevention", out var lt) ? lt.GetString() : null;
        }
        catch { /* ignore parse errors */ }

        return new LlmSummaryResult
        {
            RootCauseHypothesis = rootCause ?? content,
            LikelyComponent = component,
            ImmediateFix = immediate,
            LongTermPrevention = longTerm,
            RawModelResponse = content
        };
    }
}
