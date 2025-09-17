using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services;
using NotificationService.Application.Settings;
using NotificationService.Infrastructure.Caching;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Data.Repositories;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Services.Strategies;
using StackExchange.Redis;

namespace NotificationService.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all notification service dependencies
    /// </summary>
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure settings
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<SmsSettings>(configuration.GetSection(SmsSettings.SectionName));
        services.Configure<PushSettings>(configuration.GetSection(PushSettings.SectionName));

        // Add MongoDB
        services.AddMongoDB(configuration);

        // Add Redis
        services.AddRedis(configuration);

        // Add repositories
        services.AddRepositories();

        // Add messaging
        services.AddMessaging();

        // Add notification services
        services.AddNotificationChannelServices();

        // Add application services
        services.AddApplicationServices();

        return services;
    }

    private static IServiceCollection AddMongoDB(this IServiceCollection services, IConfiguration configuration)
    {
        var mongoSettings = new MongoDbSettings();
        configuration.GetSection(MongoDbSettings.SectionName).Bind(mongoSettings);
        
        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var client = new MongoClient(mongoSettings.ConnectionString);
            return client.GetDatabase(mongoSettings.DatabaseName);
        });

        services.AddSingleton<MongoDbContext>();

        return services;
    }

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = new RedisSettings();
        configuration.GetSection(RedisSettings.SectionName).Bind(redisSettings);;
        
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = redisSettings.ConnectionString;
            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.ConnectTimeout = redisSettings.ConnectTimeoutMs;
            configurationOptions.SyncTimeout = redisSettings.SyncTimeoutMs;
            
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register base repositories
        services.AddScoped<NotificationTemplateRepository>();
        services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();
        
        // Register cached repository as the primary interface implementation
        services.AddScoped<INotificationTemplateRepository>(provider =>
        {
            var baseRepository = provider.GetRequiredService<NotificationTemplateRepository>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var redisSettings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisSettings>>();
            
            return new CachedNotificationTemplateRepository(baseRepository, cacheService, redisSettings);
        });

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddSingleton<IMessageConsumer, RabbitMqMessageConsumer>();

        return services;
    }

    private static IServiceCollection AddNotificationChannelServices(this IServiceCollection services)
    {
        // Register individual services
        services.AddScoped<IEmailService, SendGridEmailService>();
        services.AddScoped<ISmsService, TwilioSmsService>();
        services.AddScoped<IPushService, FcmPushService>();

        // Register strategies
        services.AddScoped<EmailChannelStrategy>();
        services.AddScoped<SmsChannelStrategy>();
        services.AddScoped<PushChannelStrategy>();

        // Register factory
        services.AddScoped<INotificationChannelFactory, NotificationChannelFactory>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationProcessor, NotificationProcessor>();

        return services;
    }
}