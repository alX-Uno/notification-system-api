namespace NotificationSystem.Api.Models.Dtos;

public record OrderDto(
    Guid Id,
    string CustomerName,
    decimal TotalAmount,
    DateTime CreatedAt);