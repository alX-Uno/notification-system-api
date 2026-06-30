namespace NotificationSystem.Api.Models.Options;

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpireMinutes { get; init; } = 60;
}