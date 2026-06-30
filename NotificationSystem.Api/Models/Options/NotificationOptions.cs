namespace NotificationSystem.Api.Models.Options;

public class NotificationOptions
{
    public const string Section = "Notification";
    public int FailureRatePercent { get; init; } = 10;  // % de fallos simulados
    public int SimulatedDelayMs { get; init; } = 500;
}