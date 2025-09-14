using ErrorSummarizer.Api.Services;
using FluentAssertions;
using Xunit;

public class FakeLlmClientTests
{
    [Fact]
    public async Task InfersNullRootCause()
    {
        var client = new FakeLlmClient();
        var prompt = "Exception:\nSystem.NullReferenceException: Oops";
        var result = await client.GetSummaryAsync(prompt);
        result.RootCauseHypothesis.Should().NotBeNull();
        result.RootCauseHypothesis!.ToLowerInvariant().Should().Contain("null");
    }
}
