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