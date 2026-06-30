using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using NotificationSystem.Api.Data;
using NotificationSystem.Api.Models.Entities;
using NotificationSystem.Api.Models.Options;
using NotificationSystem.Api.Services.Abstractions;

namespace NotificationSystem.Api.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationOptions _notificationOptions;
    private readonly ResiliencePipeline<bool> _resiliencePolicy;

    public NotificationService(
        AppDbContext context,
        ILogger<NotificationService> logger,
        IOptions<NotificationOptions> notificationOptions)
    {
        _context = context;
        _logger = logger;
        _notificationOptions = notificationOptions.Value;
        _resiliencePolicy = BuildResiliencePipeline();
    }

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
            _logger.LogError("Circuit breaker abierto, notificación rechazada: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            attempt.Success = false;
            _logger.LogError("Error al enviar notificación: {Message}", ex.Message);
        }

        _context.NotificationAttempts.Add(attempt);
        await _context.SaveChangesAsync(ct);

        return attempt;
    }

    private ResiliencePipeline<bool> BuildResiliencePipeline()
    {
        var failurePredicate = new PredicateBuilder<bool>()
            .Handle<HttpRequestException>()
            .Handle<Exception>(ex => ex.Message.Contains("temporary"))
            .HandleResult(result => !result);

        return new ResiliencePipelineBuilder<bool>()
            .AddRetry(new RetryStrategyOptions<bool>
            {
                ShouldHandle = failurePredicate,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Reintentando notificación. Intento {Attempt}, esperando {Delay}s",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalSeconds
                    );
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                ShouldHandle = failurePredicate,
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                BreakDuration = TimeSpan.FromSeconds(15),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Circuit breaker ABIERTO. Sin intentos durante {Duration}s",
                        args.BreakDuration.TotalSeconds
                    );
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker CERRADO. Reanudando tráfico normal");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker HALF-OPEN. Probando con una llamada");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private async Task<bool> SendNotificationToExternalServiceAsync(Order order, CancellationToken ct)
    {
        await Task.Delay(_notificationOptions.SimulatedDelayMs, ct);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("📧 NOTIFICACIÓN ENVIADA (Mock)");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine($"  Pedido ID    : {order.Id}");
        Console.WriteLine($"  Cliente      : {order.CustomerName}");
        Console.WriteLine($"  Monto        : ${order.TotalAmount}");
        Console.WriteLine($"  Fecha (UTC)  : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.ResetColor();

        if (Random.Shared.Next(100) < _notificationOptions.FailureRatePercent)
            throw new HttpRequestException("Servicio temporalmente no disponible (simulado)");

        return true;
    }
}