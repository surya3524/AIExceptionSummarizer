namespace ErrorSummarizer.Api.Services;

public sealed class LlmSummaryResult
{
    public string? RootCauseHypothesis { get; init; }
    public string? LikelyComponent { get; init; }
    public string? ImmediateFix { get; init; }
    public string? LongTermPrevention { get; init; }
    public string? RawModelResponse { get; init; }

    public override string ToString()
        => $"RootCause: {RootCauseHypothesis}\nComponent: {LikelyComponent}\nImmediateFix: {ImmediateFix}\nPrevention: {LongTermPrevention}";
}
