using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using NotificationSystem.Api.Data;
using NotificationSystem.Api.Models.Entities;
using NotificationSystem.Api.Models.Options;
using NotificationSystem.Api.Services.Abstractions;

namespace NotificationSystem.Api.Services.Implementations;

public class NotificationService(
    AppDbContext context,
    ILogger<NotificationService> logger,
    IOptions<NotificationOptions> notificationOptions,
    ResiliencePipeline<bool> resiliencePolicy) : INotificationService
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<NotificationService> _logger = logger;
    private readonly NotificationOptions _notificationOptions = notificationOptions.Value;
    private readonly ResiliencePipeline<bool> _resiliencePolicy = resiliencePolicy;

    public async Task<NotificationAttempt> SendOrderNotificationAsync(Order order, CancellationToken ct = default)
    {
        var attempt = new NotificationAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            AttemptedAt = DateTime.UtcNow
        };

        try
        {
            attempt.Success = await _resiliencePolicy.ExecuteAsync(
                async token => await SendNotificationToExternalServiceAsync(order, token),
                ct
            );
        }
        catch (BrokenCircuitException ex)
        {
            attempt.Success = false;
            _logger.LogError("Circuit breaker opened, notification rejected: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            attempt.Success = false;
            _logger.LogError("Error sending notification: {Message}", ex.Message);
        }

        _context.NotificationAttempts.Add(attempt);
        await _context.SaveChangesAsync(ct);

        return attempt;
    }

    private async Task<bool> SendNotificationToExternalServiceAsync(Order order, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Task.Delay(_notificationOptions.SimulatedDelayMs, ct);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("📧 NOTIFICATION SENT (Mock)");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine($"  Order ID    : {order.Id}");
        Console.WriteLine($"  Customer      : {order.CustomerName}");
        Console.WriteLine($"  Amount        : ${order.TotalAmount}");
        Console.WriteLine($"  Date (UTC)  : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.ResetColor();

        if (Random.Shared.Next(100) < _notificationOptions.FailureRatePercent)
            throw new HttpRequestException("Service temporarily unavailable (simulated)");

        return true;
    }
}