namespace NotificationSystem.Api.Models.Options;

public class AuthOptions
{
    public const string Section = "Auth";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}