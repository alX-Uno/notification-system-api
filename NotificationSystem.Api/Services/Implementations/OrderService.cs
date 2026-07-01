using Microsoft.EntityFrameworkCore;
using NotificationSystem.Api.Data;
using NotificationSystem.Api.Models.Dtos;
using NotificationSystem.Api.Models.Entities;
using NotificationSystem.Api.Services.Abstractions;

namespace NotificationSystem.Api.Services.Implementations;

public class OrderService(AppDbContext context, INotificationService notificationService) : IOrderService
{
    private readonly AppDbContext _context = context;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<Order> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = dto.CustomerName,
            TotalAmount = dto.TotalAmount,
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        await _notificationService.SendOrderNotificationAsync(order, CancellationToken.None);

        return order;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.NotificationAttempts)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.NotificationAttempts)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}