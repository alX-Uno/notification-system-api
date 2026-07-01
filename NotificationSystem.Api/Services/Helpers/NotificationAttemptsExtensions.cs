using NotificationSystem.Api.Models.Dtos;
using NotificationSystem.Api.Models.Entities;

namespace NotificationSystem.Api.Services.Helpers
{
    public static class NotificationAttemptExtensions
    {
        public static NotificationAttemptDto AsDto(this NotificationAttempt attempt)
            => new(
                attempt.Id,
                attempt.OrderId,
                attempt.Success,
                attempt.AttemptedAt);
    }
}