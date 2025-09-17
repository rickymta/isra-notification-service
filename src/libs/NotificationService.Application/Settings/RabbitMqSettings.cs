namespace NotificationService.Application.Settings;

/// <summary>
/// Configuration settings for RabbitMQ connection and behavior
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// RabbitMQ host address
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Queue name for notification requests
    /// </summary>
    public string NotificationQueue { get; set; } = "notifications";

    /// <summary>
    /// Exchange name for routing
    /// </summary>
    public string Exchange { get; set; } = "notification_exchange";

    /// <summary>
    /// Routing key for notifications
    /// </summary>
    public string RoutingKey { get; set; } = "notification.send";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Socket read timeout in seconds
    /// </summary>
    public int SocketReadTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Socket write timeout in seconds
    /// </summary>
    public int SocketWriteTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Heartbeat interval in seconds
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Enable automatic recovery
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Network recovery interval in seconds
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Enable topology recovery
    /// </summary>
    public bool TopologyRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Use background threads for IO operations
    /// </summary>
    public bool UseBackgroundThreadsForIO { get; set; } = true;

    /// <summary>
    /// Dispatch consumers asynchronously
    /// </summary>
    public bool DispatchConsumersAsync { get; set; } = true;

    /// <summary>
    /// Maximum number of channels per connection
    /// </summary>
    public int MaxChannelsPerConnection { get; set; } = 2047;

    /// <summary>
    /// Maximum frame size in bytes
    /// </summary>
    public int MaxFrameSize { get; set; } = 131072;

    /// <summary>
    /// Maximum channels in connection pool
    /// </summary>
    public int MaxChannelsInPool { get; set; } = 50;

    /// <summary>
    /// Initial channel count in pool
    /// </summary>
    public int InitialChannelCount { get; set; } = 5;

    /// <summary>
    /// Prefetch count for channels
    /// </summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Maximum retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial delay for exponential backoff in milliseconds
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum delay for exponential backoff in milliseconds
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;
}