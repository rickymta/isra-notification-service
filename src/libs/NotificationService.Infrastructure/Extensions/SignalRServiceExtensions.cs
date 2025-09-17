using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.SignalR;
using NotificationService.Infrastructure.Data.Repositories;

namespace NotificationService.Infrastructure.Extensions;

/// <summary>
/// SignalR service registration extensions
/// </summary>
public static class SignalRServiceExtensions
{
    /// <summary>
    /// Add SignalR services for real-time notifications
    /// </summary>
    public static IServiceCollection AddSignalRNotifications(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add SignalR
        var signalRBuilder = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = configuration.GetValue<bool>("SignalR:EnableDetailedErrors", false);
            options.MaximumReceiveMessageSize = configuration.GetValue<long>("SignalR:MaxMessageSize", 1024 * 1024); // 1MB
            options.StreamBufferCapacity = configuration.GetValue<int>("SignalR:StreamBufferCapacity", 10);
            options.MaximumParallelInvocationsPerClient = configuration.GetValue<int>("SignalR:MaxParallelInvocations", 1);
            
            // Configure timeouts
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(
                configuration.GetValue<int>("SignalR:ClientTimeoutSeconds", 30));
            options.KeepAliveInterval = TimeSpan.FromSeconds(
                configuration.GetValue<int>("SignalR:KeepAliveSeconds", 15));
            options.HandshakeTimeout = TimeSpan.FromSeconds(
                configuration.GetValue<int>("SignalR:HandshakeTimeoutSeconds", 15));
        });

        // Configure Redis backplane if enabled
        var useRedisBackplane = configuration.GetValue<bool>("SignalR:UseRedisBackplane", false);
        if (useRedisBackplane)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                // Note: Install Microsoft.AspNetCore.SignalR.StackExchangeRedis package to enable this
                // signalRBuilder.AddStackExchangeRedis(redisConnectionString);
            }
        }

        // Register SignalR services
        services.AddScoped<IRealtimeNotificationService, SignalRRealtimeNotificationService>();
        services.AddScoped<IInAppNotificationService, InAppNotificationService>();
        
        // Register repositories
        services.AddScoped<IInAppNotificationRepository, MongoInAppNotificationRepository>();
        services.AddScoped<IUserNotificationPreferenceRepository, MongoUserNotificationPreferenceRepository>();
        
        // Register connection manager
        var useRedisConnectionManager = configuration.GetValue<bool>("SignalR:UseRedisConnectionManager", false);
        if (useRedisConnectionManager)
        {
            services.AddScoped<IConnectionManager, RedisConnectionManager>();
        }
        else
        {
            services.AddSingleton<IConnectionManager, InMemoryConnectionManager>();
        }

        return services;
    }

    /// <summary>
    /// Add SignalR message pack protocol for better performance
    /// </summary>
    public static IServiceCollection AddSignalRMessagePack(this IServiceCollection services)
    {
        // Note: Install Microsoft.AspNetCore.SignalR.Protocols.MessagePack package to enable this
        // services.AddSignalR().AddMessagePackProtocol();
            
        return services;
    }

    /// <summary>
    /// Add SignalR JSON protocol with custom options
    /// </summary>
    public static IServiceCollection AddSignalRJsonProtocol(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = 
                    System.Text.Json.JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.WriteIndented = 
                    configuration.GetValue<bool>("SignalR:JsonIndented", false);
            });
            
        return services;
    }

    /// <summary>
    /// Configure SignalR CORS policies
    /// </summary>
    public static IServiceCollection AddSignalRCors(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("SignalR:AllowedOrigins").Get<string[]>() 
            ?? new[] { "*" };

        services.AddCors(options =>
        {
            options.AddPolicy("SignalRCorsPolicy", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Add SignalR authentication and authorization
    /// </summary>
    public static IServiceCollection AddSignalRAuth(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure authentication schemes for SignalR
        services.Configure<HubOptions>(options =>
        {
            options.AddFilter<SignalRAuthFilter>();
        });

        return services;
    }

    /// <summary>
    /// Add SignalR monitoring and health checks
    /// </summary>
    public static IServiceCollection AddSignalRMonitoring(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<SignalRHealthCheck>("signalr");

        return services;
    }
}

/// <summary>
/// SignalR authentication filter
/// </summary>
public class SignalRAuthFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext, 
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // Add custom authentication logic here
        var context = invocationContext.Context;
        
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            throw new HubException("Authentication required");
        }

        return await next(invocationContext);
    }
}

/// <summary>
/// SignalR health check
/// </summary>
public class SignalRHealthCheck : IHealthCheck
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRHealthCheck(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - if we can access the hub context, we're healthy
            if (_hubContext != null)
            {
                return Task.FromResult(HealthCheckResult.Healthy("SignalR is running"));
            }
            
            return Task.FromResult(HealthCheckResult.Unhealthy("SignalR hub context is null"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("SignalR health check failed", ex));
        }
    }
}
