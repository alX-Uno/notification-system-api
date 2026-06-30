// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using NotificationSystem.Api.Models.Entities;

namespace NotificationSystem.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        //Ejemplo:
        // public DbSet<Producto> Productos { get; set; }

        // protected override void OnModelCreating(ModelBuilder modelBuilder)
        // {
        //     base.OnModelCreating(modelBuilder);
        //     // Configuraciones adicionales de entidades aquí
        // }

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<NotificationAttempt> NotificationAttempts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la relación entre Order y NotificationAttempt
            // Aquí, se establece que un Order puede tener muchos NotificationAttempts,
            // y cada NotificationAttempt pertenece a un Order.
            // Además, se establece que al eliminar un Order, se eliminarán en cascada 
            // sus NotificationAttempts asociados.
            modelBuilder.Entity<Order>()
            .HasMany(o => o.NotificationAttempts)
            .WithOne(n => n.Order)
            .HasForeignKey(n => n.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        }
    }
}