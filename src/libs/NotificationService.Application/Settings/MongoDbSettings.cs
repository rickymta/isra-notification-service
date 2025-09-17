namespace NotificationService.Application.Settings;

/// <summary>
/// Configuration settings for MongoDB connection
/// </summary>
public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    /// <summary>
    /// MongoDB connection string
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = "NotificationService";

    /// <summary>
    /// Collection name for notification templates
    /// </summary>
    public string TemplatesCollection { get; set; } = "notification_templates";

    /// <summary>
    /// Collection name for notification history
    /// </summary>
    public string HistoryCollection { get; set; } = "notification_history";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Socket timeout in seconds
    /// </summary>
    public int SocketTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum connection pool size
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Minimum connection pool size
    /// </summary>
    public int MinConnectionPoolSize { get; set; } = 25;

    /// <summary>
    /// Maximum connection idle time in minutes
    /// </summary>
    public int MaxConnectionIdleTimeMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum connection lifetime in hours
    /// </summary>
    public int MaxConnectionLifeTimeHours { get; set; } = 1;

    /// <summary>
    /// Server selection timeout in seconds
    /// </summary>
    public int ServerSelectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Wait queue timeout in seconds
    /// </summary>
    public int WaitQueueTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Heartbeat interval in seconds
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Heartbeat timeout in seconds
    /// </summary>
    public int HeartbeatTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Enable retry writes
    /// </summary>
    public bool RetryWrites { get; set; } = true;

    /// <summary>
    /// Enable retry reads
    /// </summary>
    public bool RetryReads { get; set; } = true;
}