namespace NotificationSystem.Api.Models.Dtos
{
    public record OrderDetailsDto(
        Guid Id,
        string CustomerName,
        decimal TotalAmount,
        DateTime CreatedAt,
        IEnumerable<NotificationAttemptDto> NotificationAttempts);
}