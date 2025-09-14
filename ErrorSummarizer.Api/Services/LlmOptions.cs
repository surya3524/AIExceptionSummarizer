namespace ErrorSummarizer.Api.Services;

public sealed class LlmOptions
{
    public bool Enabled { get; set; } = false;
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 8;
}
