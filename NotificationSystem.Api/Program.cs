using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NotificationSystem.Api.Data;
using NotificationSystem.Api.Models.Options;
using NotificationSystem.Api.Services.Abstractions;
using NotificationSystem.Api.Services.Implementations;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration, builder.Environment.IsDevelopment());

var app = builder.Build();

ConfigurePipeline(app);

app.Run();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
{
    services.AddOpenApi();

    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

    services.AddControllers();
    services.AddValidation();

    services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
    services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.Section));
    services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.Section));

    services.AddSingleton<ResiliencePipeline<bool>>(sp => BuildNotificationPipeline(sp));
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<INotificationService, NotificationService>();

    var jwtOptions = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration is required.");

    if (string.IsNullOrWhiteSpace(jwtOptions.Key))
    {
        throw new InvalidOperationException("JWT signing key cannot be empty.");
    }

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.IncludeErrorDetails = isDevelopment;
            options.RequireHttpsMetadata = !isDevelopment;
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
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
                }));

        options.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await ctx.HttpContext.Response.WriteAsync("Too many requests, try again later", ct);
        };
    });

    services.AddAuthorization();
}

static ResiliencePipeline<bool> BuildNotificationPipeline(IServiceProvider sp)
{
    var logger = sp.GetRequiredService<ILogger<NotificationService>>();
    var options = sp.GetRequiredService<IOptions<NotificationOptions>>().Value;
    var resilience = options.Resilience;

    var failurePredicate = new PredicateBuilder<bool>()
        .Handle<HttpRequestException>()
        .Handle<Exception>(ex => ex.Message.Contains("temporary", StringComparison.OrdinalIgnoreCase))
        .HandleResult(result => !result);

    return new ResiliencePipelineBuilder<bool>()
        .AddRetry(new RetryStrategyOptions<bool>
        {
            ShouldHandle = failurePredicate,
            MaxRetryAttempts = resilience.MaxRetryAttempts,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                logger.LogWarning(
                    "Retrying notifications. Attempt {Attempt}, waiting for {Delay}s",
                    args.AttemptNumber + 1,
                    args.RetryDelay.TotalSeconds);
                return ValueTask.CompletedTask;
            }
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
        {
            ShouldHandle = failurePredicate,
            FailureRatio = resilience.FailureRatio,
            MinimumThroughput = resilience.MinimumThroughput,
            BreakDuration = TimeSpan.FromSeconds(resilience.BreakDurationSeconds),
            OnOpened = args =>
            {
                logger.LogWarning(
                    "Circuit breaker transitioned to Open state. Blocking calls for {Duration}s",
                    args.BreakDuration.TotalSeconds);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                logger.LogInformation("Circuit breaker transitioned to Closed state. Resuming normal traffic.");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                logger.LogInformation("Circuit breaker transitioned to Half-Open state. Testing the next call.");
                return ValueTask.CompletedTask;
            }
        })
        .Build();
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

public partial class Program { }