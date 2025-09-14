using ErrorSummarizer.Api.Services;
using ErrorSummarizer.Api.Options;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Xunit;

public class LlmErrorSummarizerTests
{
    private class TestClient : ILlmClient
    {
        public Task<LlmSummaryResult> GetSummaryAsync(string prompt, CancellationToken ct = default)
            => Task.FromResult(new LlmSummaryResult {
                RootCauseHypothesis = "Cause",
                LikelyComponent = "Comp",
                ImmediateFix = "Fix",
                LongTermPrevention = "Prevent",
                RawModelResponse = prompt
            });
    }

    [Fact]
    public async Task Disabled_ReturnsBasicMessage()
    {
        var opts = Options.Create(new LlmOptions { Enabled = false });
        var sut = new LlmOnlyErrorSummarizer(new TestClient(), opts);

        var ex = new Exception("Boom");
        var ctx = DummyContext();
        var summary = await sut.SummarizeAsync(ex, ctx);

        summary.Should().Contain("LLM disabled");
    }

    [Fact]
    public async Task Enabled_IncludesAiSection()
    {
        var opts = Options.Create(new LlmOptions { Enabled = true, TimeoutSeconds = 5 });
        var sut = new LlmOnlyErrorSummarizer(new TestClient(), opts);

        var summary = await sut.SummarizeAsync(new Exception("HttpStatusCode.NotFound"), DummyContext());
        summary.Should().Contain("--- AI Analysis ---").And.Contain("RootCauseHypothesis");
    }

    private static ErrorContext DummyContext() => new()
    {
        CorrelationId = "cid",
        Method = "POST",
        Route = "/x",
        Headers = new Dictionary<string,string>(),
        Claims = new Dictionary<string,string>(),
        SanitizedBody = string.Empty,
        EnvironmentName = "Test",
        UserAgent = "UA",
        TopStackFrame = null,
        TopAppFrames = null
    };
}
