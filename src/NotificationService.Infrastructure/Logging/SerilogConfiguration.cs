using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NotificationService.Application.Settings;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace NotificationService.Infrastructure.Logging;

/// <summary>
/// Serilog configuration helper
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configure Serilog for the application
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="environment">Hosting environment</param>
    /// <returns>Configured Serilog logger configuration</returns>
    public static LoggerConfiguration ConfigureSerilog(IConfiguration configuration, IHostEnvironment environment)
    {
        var loggingSettings = new LoggingSettings();
        configuration.GetSection(LoggingSettings.SectionName).Bind(loggingSettings);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(GetLogLevel(loggingSettings.LogLevel.Default))
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", loggingSettings.Application.ServiceName)
            .Enrich.WithProperty("Environment", loggingSettings.Application.Environment)
            .Enrich.WithProperty("Version", loggingSettings.Application.Version)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

        // Add Elasticsearch sink if enabled
        if (loggingSettings.Elasticsearch.Enabled && !string.IsNullOrEmpty(loggingSettings.Elasticsearch.Url))
        {
            var elasticsearchOptions = new ElasticsearchSinkOptions(new Uri(loggingSettings.Elasticsearch.Url))
            {
                IndexFormat = loggingSettings.Elasticsearch.IndexFormat,
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                TemplateName = "notification-service-template",
                NumberOfShards = 2,
                NumberOfReplicas = 1,
                BufferBaseFilename = "./logs/buffer",
                BufferLogShippingInterval = TimeSpan.FromSeconds(5)
            };

            // Add authentication if provided
            if (!string.IsNullOrEmpty(loggingSettings.Elasticsearch.Username))
            {
                elasticsearchOptions.ModifyConnectionSettings = x => x.BasicAuthentication(
                    loggingSettings.Elasticsearch.Username, 
                    loggingSettings.Elasticsearch.Password);
            }

            loggerConfig.WriteTo.Elasticsearch(elasticsearchOptions);
        }

        // Add file logging for production
        if (environment.IsProduction())
        {
            loggerConfig.WriteTo.File(
                path: "./logs/notification-service-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
        }

        return loggerConfig;
    }

    private static LogEventLevel GetLogLevel(string logLevel)
    {
        return logLevel.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}