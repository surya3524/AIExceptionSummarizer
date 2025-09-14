namespace ErrorSummarizer.Api.Services;

/// <summary>
/// Captured, sanitized execution context for error summarization (safe to log / send to AI).
/// </summary>
public sealed class ErrorContext
{
    public required string CorrelationId { get; init; }
    public required string Route { get; init; }
    public required string Method { get; init; }
    public IDictionary<string,string>? Headers { get; init; }
    public IDictionary<string,string>? Claims { get; init; }
    public string? SanitizedBody { get; init; }
    public string? EnvironmentName { get; init; }
    public string? UserAgent { get; init; }
    public string? TopStackFrame { get; init; }
    public IReadOnlyList<string>? TopAppFrames { get; init; }
}
