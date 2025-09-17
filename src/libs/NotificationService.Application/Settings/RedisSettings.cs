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

    /// <summary>
    /// Async timeout in milliseconds
    /// </summary>
    public int AsyncTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Response timeout in milliseconds
    /// </summary>
    public int ResponseTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Keep alive interval in seconds
    /// </summary>
    public int KeepAliveSeconds { get; set; } = 60;

    /// <summary>
    /// Connection retry count
    /// </summary>
    public int ConnectRetry { get; set; } = 3;

    /// <summary>
    /// Initial retry delay in milliseconds for exponential backoff
    /// </summary>
    public int InitialRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Abort on connection failure
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    /// Include performance counters in exceptions
    /// </summary>
    public bool IncludePerformanceCounters { get; set; } = true;

    /// <summary>
    /// Include detailed information in exceptions
    /// </summary>
    public bool IncludeDetailInExceptions { get; set; } = true;
}