using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NotificationSystem.Api.Data;
using NotificationSystem.Api.Models.Dtos;
using NotificationSystem.Api.Models.Entities;
using NotificationSystem.Api.Services.Abstractions;
using NotificationSystem.Api.Services.Implementations;
using NSubstitute;

namespace NotificationSystem.Tests.Unit;

public class OrderServiceTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    // Cada prueba obtiene una BD en memoria independiente
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // nombre único por prueba
            .Options;
        return new AppDbContext(options);
    }

    // Crea el servicio bajo prueba con sus dependencias
    private static (OrderService svc, INotificationService notificationMock, AppDbContext db) CreateSut()
    {
        var db = CreateInMemoryDb();

        // NSubstitute crea un "doble" de INotificationService
        // que no hace nada real
        var notificationMock = Substitute.For<INotificationService>();

        // Por defecto, el mock retorna un intento exitoso
        notificationMock
            .SendOrderNotificationAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationAttempt { Success = true });

        var svc = new OrderService(db, notificationMock);
        return (svc, notificationMock, db);
    }

    // ─── Pruebas de CreateOrder ───────────────────────────────────────────────

    [Fact]
    public async Task CreateOrder_GuardaOrdenEnBd()
    {
        // Arrange — preparar todo lo necesario
        var (svc, _, db) = CreateSut();
        var dto = new CreateOrderDto { CustomerName = "Ana", TotalAmount = 99.99m };

        // Act — ejecutar la acción bajo prueba
        var result = await svc.CreateOrderAsync(dto);

        // Assert — verificar el resultado esperado
        var orderInBd = await db.Orders.FindAsync(result.Id);

        Assert.NotNull(orderInBd);
        Assert.Equal("Ana", orderInBd.CustomerName);
        Assert.Equal(99.99m, orderInBd.TotalAmount);
        Assert.NotEqual(Guid.Empty, orderInBd.Id);
    }

    [Fact]
    public async Task CreateOrder_InvokesNotificationService()
    {
        // Arrange
        var (svc, notificationMock, _) = CreateSut();
        var dto = new CreateOrderDto { CustomerName = "Carlos", TotalAmount = 50m };

        // Act
        var order = await svc.CreateOrderAsync(dto);

        // Assert — verifica que el mock fue llamado exactamente 1 vez con la orden creada
        await notificationMock
            .Received(1) // "debe haber recibido exactamente 1 llamada"
            .SendOrderNotificationAsync(
                Arg.Is<Order>(o => o.Id == order.Id), // con esta orden
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task CreateOrder_WhenNotificationFails_OrderIsSaved()
    {
        // Arrange — configurar el mock para que falle
        var (svc, notificationMock, db) = CreateSut();

        notificationMock
            .SendOrderNotificationAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationAttempt { Success = false });

        var dto = new CreateOrderDto { CustomerName = "Luis", TotalAmount = 10m };

        // Act
        var result = await svc.CreateOrderAsync(dto);

        // Assert — la orden existe en BD independientemente del resultado de la notificación
        var orderInBd = await db.Orders.FindAsync(result.Id);
        Assert.NotNull(orderInBd);
    }

    // ─── Pruebas de GetById ───────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ShowsNotificationHistory()
    {
        // Arrange — insertar datos directamente en la BD de prueba
        var (svc, _, db) = CreateSut();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = "María",
            TotalAmount = 200m,
            CreatedAt = DateTime.UtcNow,
            NotificationAttempts =
            [
                new() { Id = Guid.NewGuid(), Success = false, AttemptedAt = DateTime.UtcNow.AddMinutes(-2) },
                new() { Id = Guid.NewGuid(), Success = true,  AttemptedAt = DateTime.UtcNow.AddMinutes(-1) }
            ]
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("María", result.CustomerName);
        Assert.Equal(2, result.NotificationAttempts.Count);
        Assert.Contains(result.NotificationAttempts, a => a.Success);    // tiene uno exitoso
        Assert.Contains(result.NotificationAttempts, a => !a.Success);   // y uno fallido
    }

    [Fact]
    public async Task GetById_WhenDoesNotExist_ReturnsNull()
    {
        var (svc, _, _) = CreateSut();

        var result = await svc.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ─── Pruebas de GetAll ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_PagesCorrectly()
    {
        // Arrange — insertar 5 órdenes
        var (svc, _, db) = CreateSut();

        var orders = Enumerable.Range(1, 5).Select(i => new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = $"Cliente {i}",
            TotalAmount = i * 10m,
            CreatedAt = DateTime.UtcNow.AddMinutes(-i)
        });

        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();

        // Act — pedir página 2 con 2 elementos por página
        var page2 = (await svc.GetAllAsync(page: 2, pageSize: 2)).ToList();

        // Assert
        Assert.Equal(2, page2.Count); // exactamente 2 en la página 2
    }

    [Fact]
    public async Task GetAll_ReturnsOrdersInDescendingOrderByDate()
    {
        var (svc, _, db) = CreateSut();

        var ahora = DateTime.UtcNow;
        db.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), CustomerName = "Vieja", TotalAmount = 1m, CreatedAt = ahora.AddHours(-2) },
            new Order { Id = Guid.NewGuid(), CustomerName = "Reciente", TotalAmount = 1m, CreatedAt = ahora }
        );
        await db.SaveChangesAsync();

        var result = (await svc.GetAllAsync()).ToList();

        // La primera debe ser la más reciente
        Assert.Equal("Reciente", result[0].CustomerName);
        Assert.Equal("Vieja", result[1].CustomerName);
    }
}