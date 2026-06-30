using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotificationSystem.Api.Data;
using NotificationSystem.Api.Models.Options;
using NotificationSystem.Api.Services.Abstractions;
using NotificationSystem.Api.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigurePipeline(app);

app.Run();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddOpenApi();

    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection")
        ));

    services.AddControllers();
    services.AddValidation();

    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<INotificationService, NotificationService>();

    services.Configure<JwtOptions>(
        configuration.GetSection(JwtOptions.Section));

    services.Configure<NotificationOptions>(
        configuration.GetSection(NotificationOptions.Section));

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtOptions = configuration
                .GetSection(JwtOptions.Section)
                .Get<JwtOptions>() ?? new JwtOptions();

            options.IncludeErrorDetails = true;
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Key)
                )
            };
        });

    services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }
            ));

        options.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await ctx.HttpContext.Response.WriteAsync("Demasiadas solicitudes, intenta más tarde", ct);
        };
    });

    services.AddAuthorization();
}

static void ConfigurePipeline(WebApplication app)
{
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
}