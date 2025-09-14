namespace ErrorSummarizer.Api.Options;

public sealed class LlmOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-5";
    public int TimeoutSeconds { get; set; } = 30;
}