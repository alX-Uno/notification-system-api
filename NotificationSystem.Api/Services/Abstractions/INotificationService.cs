using NotificationSystem.Api.Models.Entities;

namespace NotificationSystem.Api.Services.Abstractions;

public interface INotificationService
{
    Task<NotificationAttempt> SendOrderNotificationAsync(Order order, CancellationToken ct = default);
}