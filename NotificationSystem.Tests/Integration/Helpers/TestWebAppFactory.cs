using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NotificationSystem.Api.Data;

namespace NotificationSystem.Tests.Integration.Helpers;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remover todo lo relacionado con DbContext y sus providers internos
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // Remover cualquier servicio cuyo tipo venga del namespace de EF SqlServer
            var sqlServerDescriptors = services
                .Where(d => d.ServiceType.Assembly.FullName?
                    .Contains("EntityFrameworkCore.SqlServer") == true)
                .ToList();

            foreach (var d in sqlServerDescriptors)
                services.Remove(d);

            // Registrar con InMemory + nuevo service provider interno
            services.AddDbContext<AppDbContext>(options =>
                options
                    .UseInMemoryDatabase("IntegrationTestDb")
                    .UseInternalServiceProvider(
                        new ServiceCollection()
                            .AddEntityFrameworkInMemoryDatabase()
                            .BuildServiceProvider()
                    )
            );
        });
    }
}