namespace NotificationService.Application.Settings;

/// <summary>
/// Configuration settings for Redis caching
/// </summary>
public class RedisSettings
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Database number to use
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Key prefix for this application
    /// </summary>
    public string KeyPrefix { get; set; } = "NotificationService:";

    /// <summary>
    /// Default expiration time for cached items in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Template cache expiration in minutes
    /// </summary>
    public int TemplateCacheExpirationMinutes { get; set; } = 120;

    /// <summary>
    /// User preferences cache expiration in minutes
    /// </summary>
    public int UserPreferencesCacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Connect timeout in milliseconds
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 5000;
}