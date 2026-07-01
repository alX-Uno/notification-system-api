namespace NotificationSystem.Api.Models.Dtos;

public record NotificationAttemptDto(
    Guid Id,
    Guid OrderId,
    bool Success,
    DateTime AttemptedAt);