namespace NotificationService.Application.Settings;

/// <summary>
/// Logging settings for the application
/// </summary>
public class LoggingSettings
{
    public const string SectionName = "Logging";

    public LogLevelSettings LogLevel { get; set; } = new();
    public ElasticsearchSettings Elasticsearch { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();

    public class LogLevelSettings
    {
        public string Default { get; set; } = "Information";
    }

    public class ElasticsearchSettings
    {
        public bool Enabled { get; set; } = false;
        public string Url { get; set; } = "http://localhost:9200";
        public string IndexFormat { get; set; } = "notification-service-logs-{0:yyyy.MM.dd}";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class ApplicationSettings
    {
        public string ServiceName { get; set; } = "NotificationService";
        public string Environment { get; set; } = "Development";
        public string Version { get; set; } = "1.0.0";
    }
}