using NotificationSystem.Api.Models.Dtos;
using NotificationSystem.Api.Models.Entities;

namespace NotificationSystem.Api.Services.Abstractions;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderDto createOrderDto, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
}