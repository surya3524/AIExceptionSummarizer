using ErrorSummarizer.Api.Services;
using FluentAssertions;
using Xunit;

public class HeuristicSummarizerTests
{
    [Fact]
    public async Task AddsNullHint()
    {
        var sut = new BasicHeuristicErrorSummarizer();
        var ex = new NullReferenceException("Object reference not set to an instance of an object.");
        var ctx = DummyContext();
        var result = await sut.SummarizeAsync(ex, ctx);
        result.Should().Contain("Possible Cause").And.Contain("null");
    }

    [Fact]
    public async Task IncludesInnerException()
    {
        var sut = new BasicHeuristicErrorSummarizer();
        var ex = new ApplicationException("Wrapper", new TimeoutException("Timed out"));
        var ctx = DummyContext();
        var result = await sut.SummarizeAsync(ex, ctx);
        result.Should().Contain("Inner: TimeoutException");
    }

    private static ErrorContext DummyContext() => new()
    {
        CorrelationId = "abc",
        Method = "GET",
        Route = "/test",
        Headers = new Dictionary<string,string>(),
        Claims = new Dictionary<string,string>(),
        SanitizedBody = string.Empty,
        EnvironmentName = "Test",
        UserAgent = "UA",
        TopStackFrame = "at Some.Method() in File.cs:line 10",
        TopAppFrames = new List<string>()
    };
}
