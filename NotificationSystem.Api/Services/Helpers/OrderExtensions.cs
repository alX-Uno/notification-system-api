namespace NotificationSystem.Api.Services.Helpers;

using NotificationSystem.Api.Models.Dtos;
using NotificationSystem.Api.Models.Entities;

public static class OrderExtensions
{
    public static OrderDto AsDto(this Order order)
        => new(
            order.Id,
            order.CustomerName,
            order.TotalAmount,
            order.CreatedAt);

    public static OrderDetailsDto AsDetailsDto(this Order order)
        => new(
            order.Id,
            order.CustomerName,
            order.TotalAmount,
            order.CreatedAt,
            order.NotificationAttempts.Select(a => a.AsDto()));
}