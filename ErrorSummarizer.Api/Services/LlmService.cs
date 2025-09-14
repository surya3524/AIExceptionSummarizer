using System.Net.Http.Json;
using ErrorSummarizer.Api.Options;
using Microsoft.Extensions.Options;

public sealed class LlmService
{
    private readonly IHttpClientFactory _factory;
    private readonly LlmOptions _opts;

    public LlmService(IHttpClientFactory factory, IOptions<LlmOptions> opts)
    {
        _factory = factory;
        _opts = opts.Value;
    }

    public async Task<string> SummarizeAsync(string input, CancellationToken ct = default)
    {
        if (!_opts.Enabled) return "LLM disabled.";

        var client = _factory.CreateClient("LlmClient");

        var payload = new
        {
            model = _opts.Model,
            messages = new[]
            {
                new {
                    role = "system",
                    content =
                        "You are an assistant that summarizes and explains software errors, including build/compilation errors, runtime errors, unhandled exceptions, stack traces, and diagnostic logs. " +
                        "For each input: 1) Brief summary, 2) Probable root cause(s), 3) Key evidence (lines, messages), 4) Recommended next steps. " +
                        "Be concise, avoid unfounded speculation, and do not invent data."
                },
                new { role = "user", content = input }
            }
        };

        using var resp = await client.PostAsJsonAsync("chat/completions", payload, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStreamAsync(ct));
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? "";
    }
}