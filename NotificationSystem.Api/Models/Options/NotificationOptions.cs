namespace NotificationSystem.Api.Models.Options;

public class NotificationOptions
{
    public const string Section = "Notification";

    public int FailureRatePercent { get; init; } = 10;
    public int SimulatedDelayMs { get; init; } = 500;

    public ResilienceOptions Resilience { get; init; } = new();
}

public class ResilienceOptions
{
    public int MaxRetryAttempts { get; init; } = 3;
    public int BreakDurationSeconds { get; init; } = 15;
    public double FailureRatio { get; init; } = 0.5;
    public int MinimumThroughput { get; init; } = 4;
}