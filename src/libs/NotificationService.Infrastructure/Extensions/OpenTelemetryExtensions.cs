using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace NotificationService.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry distributed tracing
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Service name for telemetry
    /// </summary>
    public const string ServiceName = "notification-service";
    
    /// <summary>
    /// Service version for telemetry
    /// </summary>
    public const string ServiceVersion = "1.0.0";

    /// <summary>
    /// Activity source for custom spans
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    /// <summary>
    /// Add OpenTelemetry distributed tracing to the service collection
    /// </summary>
    public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration configuration)
    {
        var tracingSettings = configuration.GetSection("Tracing").Get<TracingSettings>() ?? new TracingSettings();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(ServiceName, ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.instance.id"] = Environment.MachineName,
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ServiceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                        {
                            // Don't trace health checks and metrics endpoints
                            var path = httpContext.Request.Path.Value?.ToLowerInvariant();
                            return path != "/health" && path != "/metrics";
                        };
                        options.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            activity.SetTag("http.request.client_ip", GetClientIpAddress(httpRequest));
                            activity.SetTag("http.request.user_agent", httpRequest.Headers["User-Agent"].ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            activity.SetTag("http.response.content_length", httpResponse.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                        {
                            activity.SetTag("http.client.request.url", httpRequestMessage.RequestUri?.ToString());
                        };
                        options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                        {
                            activity.SetTag("http.client.response.status_code", (int)httpResponseMessage.StatusCode);
                        };
                    });

                // Configure exporters based on settings
                if (tracingSettings.Jaeger.Enabled)
                {
                    tracing.AddJaegerExporter(options =>
                    {
                        options.Endpoint = new Uri(tracingSettings.Jaeger.Endpoint);
                    });
                }

                if (tracingSettings.Zipkin.Enabled)
                {
                    tracing.AddZipkinExporter(options =>
                    {
                        options.Endpoint = new Uri(tracingSettings.Zipkin.Endpoint);
                    });
                }

                if (tracingSettings.Console.Enabled)
                {
                    tracing.AddConsoleExporter();
                }
            });

        return services;
    }

    private static string? GetClientIpAddress(HttpRequest request)
    {
        // Check for forwarded IP first (for load balancers)
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP (for CDNs)
        var realIp = request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// Configuration settings for distributed tracing
/// </summary>
public class TracingSettings
{
    public const string SectionName = "Tracing";

    /// <summary>
    /// Jaeger exporter settings
    /// </summary>
    public JaegerSettings Jaeger { get; set; } = new();

    /// <summary>
    /// Zipkin exporter settings
    /// </summary>
    public ZipkinSettings Zipkin { get; set; } = new();

    /// <summary>
    /// Console exporter settings
    /// </summary>
    public ConsoleExporterSettings Console { get; set; } = new();
}

/// <summary>
/// Jaeger exporter configuration
/// </summary>
public class JaegerSettings
{
    /// <summary>
    /// Whether Jaeger export is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Jaeger endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:14268/api/traces";
}

/// <summary>
/// Zipkin exporter configuration
/// </summary>
public class ZipkinSettings
{
    /// <summary>
    /// Whether Zipkin export is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Zipkin endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:9411/api/v2/spans";
}

/// <summary>
/// Console exporter configuration
/// </summary>
public class ConsoleExporterSettings
{
    /// <summary>
    /// Whether console export is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}
