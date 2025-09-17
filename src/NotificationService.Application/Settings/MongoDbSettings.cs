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
}