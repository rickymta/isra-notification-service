using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NotificationService.Application.Settings;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace NotificationService.Infrastructure.Extensions;

/// <summary>
/// Extension methods for optimized connection pooling configuration
/// </summary>
public static class ConnectionPoolingExtensions
{
    /// <summary>
    /// Add optimized MongoDB connection with advanced pooling settings
    /// </summary>
    public static IServiceCollection AddOptimizedMongoDB(this IServiceCollection services, IConfiguration configuration)
    {
        var mongoSettings = new MongoDbSettings();
        configuration.GetSection(MongoDbSettings.SectionName).Bind(mongoSettings);
        
        services.AddSingleton<IMongoClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IMongoClient>>();
            
            var mongoUrl = MongoUrl.Create(mongoSettings.ConnectionString);
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
            
            // Enhanced connection pooling settings
            clientSettings.MaxConnectionPoolSize = mongoSettings.MaxConnectionPoolSize;
            clientSettings.MinConnectionPoolSize = Math.Max(1, mongoSettings.MaxConnectionPoolSize / 4); // 25% of max
            clientSettings.MaxConnectionIdleTime = TimeSpan.FromMinutes(30);
            clientSettings.MaxConnectionLifeTime = TimeSpan.FromHours(1);
            clientSettings.WaitQueueTimeout = TimeSpan.FromSeconds(30);
            
            // Connection timeouts
            clientSettings.ConnectTimeout = TimeSpan.FromSeconds(mongoSettings.ConnectionTimeoutSeconds);
            clientSettings.SocketTimeout = TimeSpan.FromSeconds(mongoSettings.SocketTimeoutSeconds);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
            
            // Heartbeat and monitoring
            clientSettings.HeartbeatInterval = TimeSpan.FromSeconds(10);
            clientSettings.HeartbeatTimeout = TimeSpan.FromSeconds(10);
            
            // Retry policy
            clientSettings.RetryWrites = true;
            clientSettings.RetryReads = true;
            
            logger.LogInformation("MongoDB client configured with optimized connection pooling: Max={MaxPool}, Min={MinPool}", 
                clientSettings.MaxConnectionPoolSize, clientSettings.MinConnectionPoolSize);
            
            return new MongoClient(clientSettings);
        });

        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var client = provider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoSettings.DatabaseName);
        });

        return services;
    }

    /// <summary>
    /// Add optimized Redis connection with advanced pooling and performance settings
    /// </summary>
    public static IServiceCollection AddOptimizedRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = new RedisSettings();
        configuration.GetSection(RedisSettings.SectionName).Bind(redisSettings);
        
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();
            
            var configurationOptions = ConfigurationOptions.Parse(redisSettings.ConnectionString);
            
            // Connection pooling and performance optimizations
            configurationOptions.ConnectTimeout = redisSettings.ConnectTimeoutMs;
            configurationOptions.SyncTimeout = redisSettings.SyncTimeoutMs;
            configurationOptions.AsyncTimeout = redisSettings.SyncTimeoutMs;
            
            // Connection lifecycle management
            configurationOptions.KeepAlive = 60; // Send keepalive every 60 seconds
            configurationOptions.ConnectRetry = 3;
            configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(1000); // Start with 1 second, exponential backoff
            
            // Performance optimizations
            configurationOptions.AbortOnConnectFail = false; // Don't abort on initial connection failure
            configurationOptions.AllowAdmin = false; // Security: disable admin commands
            configurationOptions.ClientName = "NotificationService";
            
            // Connection pool settings
            configurationOptions.ChannelPrefix = RedisChannel.Literal(redisSettings.KeyPrefix);
            configurationOptions.DefaultDatabase = redisSettings.Database;
            
            // Async operation optimization
            configurationOptions.IncludeDetailInExceptions = true;
            configurationOptions.IncludePerformanceCountersInExceptions = true;
            
            logger.LogInformation("Redis connection configured with optimized pooling and performance settings");
            
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        return services;
    }

    /// <summary>
    /// Add optimized RabbitMQ connection factory with connection pooling
    /// </summary>
    public static IServiceCollection AddOptimizedRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitSettings = new RabbitMqSettings();
        configuration.GetSection(RabbitMqSettings.SectionName).Bind(rabbitSettings);
        
        // Register connection factory as singleton for connection pooling
        services.AddSingleton<IConnectionFactory>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IConnectionFactory>>();
            
            var factory = new ConnectionFactory
            {
                HostName = rabbitSettings.Host,
                Port = rabbitSettings.Port,
                UserName = rabbitSettings.Username,
                Password = rabbitSettings.Password,
                VirtualHost = rabbitSettings.VirtualHost,
                
                // Connection pooling and performance settings
                RequestedConnectionTimeout = TimeSpan.FromSeconds(rabbitSettings.ConnectionTimeoutSeconds),
                SocketReadTimeout = TimeSpan.FromSeconds(30),
                SocketWriteTimeout = TimeSpan.FromSeconds(30),
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                
                // Performance optimizations
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                TopologyRecoveryEnabled = true,
                
                // Dispatch consumers asynchronously
                DispatchConsumersAsync = true,
                
                // Connection limits
                RequestedChannelMax = 2047, // Max channels per connection (RabbitMQ default)
                RequestedFrameMax = 131072, // 128KB frame size
                
                // Client properties for monitoring
                ClientProvidedName = "NotificationService"
            };
            
            logger.LogInformation("RabbitMQ connection factory configured with optimized pooling settings");
            
            return factory;
        });

        // Register connection pool manager
        services.AddSingleton<IRabbitMqConnectionPool, RabbitMqConnectionPool>();

        return services;
    }
}

/// <summary>
/// Interface for RabbitMQ connection pool management
/// </summary>
public interface IRabbitMqConnectionPool : IDisposable
{
    IConnection GetConnection();
    IModel GetChannel();
    void ReturnChannel(IModel channel);
    Task<IModel> GetChannelAsync();
}

/// <summary>
/// RabbitMQ connection pool implementation for efficient connection management
/// </summary>
public class RabbitMqConnectionPool : IRabbitMqConnectionPool
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqConnectionPool> _logger;
    private readonly ConcurrentQueue<IModel> _channelPool;
    private readonly SemaphoreSlim _semaphore;
    
    private IConnection? _connection;
    private readonly object _connectionLock = new();
    private bool _disposed;
    
    private const int MaxChannelsInPool = 50;
    private const int InitialChannelCount = 5;
    
    public RabbitMqConnectionPool(IConnectionFactory connectionFactory, ILogger<RabbitMqConnectionPool> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _channelPool = new ConcurrentQueue<IModel>();
        _semaphore = new SemaphoreSlim(MaxChannelsInPool, MaxChannelsInPool);
        
        // Pre-create initial channels
        Task.Run(InitializeChannelPool);
    }

    public IConnection GetConnection()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqConnectionPool));
            
        if (_connection?.IsOpen == true)
            return _connection;

        lock (_connectionLock)
        {
            if (_connection?.IsOpen == true)
                return _connection;

            try
            {
                _connection?.Dispose();
                _connection = _connectionFactory.CreateConnection();
                _logger.LogInformation("New RabbitMQ connection established");
                
                // Handle connection shutdown
                _connection.ConnectionShutdown += (sender, args) =>
                {
                    _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", args.ReplyText);
                    ClearChannelPool();
                };
                
                return _connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create RabbitMQ connection");
                throw;
            }
        }
    }

    public IModel GetChannel()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqConnectionPool));

        // Try to get a channel from the pool first
        if (_channelPool.TryDequeue(out var pooledChannel) && pooledChannel.IsOpen)
        {
            return pooledChannel;
        }

        // Create a new channel if pool is empty or channel is closed
        try
        {
            var connection = GetConnection();
            var channel = connection.CreateModel();
            
            // Configure channel settings
            channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
            
            _logger.LogDebug("Created new RabbitMQ channel");
            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ channel");
            throw;
        }
    }

    public async Task<IModel> GetChannelAsync()
    {
        await _semaphore.WaitAsync();
        
        try
        {
            return GetChannel();
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    public void ReturnChannel(IModel channel)
    {
        if (_disposed || channel?.IsOpen != true)
        {
            channel?.Dispose();
            _semaphore.Release();
            return;
        }

        if (_channelPool.Count < MaxChannelsInPool)
        {
            _channelPool.Enqueue(channel);
        }
        else
        {
            channel.Dispose();
        }
        
        _semaphore.Release();
    }

    private void InitializeChannelPool()
    {
        try
        {
            for (int i = 0; i < InitialChannelCount; i++)
            {
                var channel = GetChannel();
                _channelPool.Enqueue(channel);
            }
            
            _logger.LogInformation("RabbitMQ channel pool initialized with {Count} channels", InitialChannelCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize RabbitMQ channel pool");
        }
    }

    private void ClearChannelPool()
    {
        while (_channelPool.TryDequeue(out var channel))
        {
            try
            {
                channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing channel from pool");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        ClearChannelPool();
        
        try
        {
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ connection");
        }
        
        _semaphore?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
