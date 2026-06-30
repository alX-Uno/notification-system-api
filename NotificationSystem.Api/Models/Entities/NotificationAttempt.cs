namespace NotificationSystem.Api.Models.Entities;

public class NotificationAttempt
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public bool Success { get; set; }
    public DateTime AttemptedAt { get; set; }
}
