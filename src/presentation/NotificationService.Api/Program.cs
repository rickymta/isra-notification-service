using NotificationService.Infrastructure.Extensions;
using NotificationService.Infrastructure.Logging;
using NotificationService.Api.Swagger;
using Prometheus;
using Serilog;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;

// Configure Serilog
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var hostEnvironment = new HostEnvironment { EnvironmentName = environment };
Log.Logger = SerilogConfiguration.ConfigureSerilog(configuration, hostEnvironment).CreateLogger();

try
{
    Log.Information("Starting NotificationService.Api");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    // Add OpenAPI/Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { 
            Title = "Notification Service API", 
            Version = "v1",
            Description = @"A comprehensive microservice for sending notifications via Email, SMS, Push, and In-App channels with real-time SignalR support.

## Features
- Multi-channel notifications (Email, SMS, Push, In-App)
- Real-time notifications via SignalR WebSocket
- Template-based messaging
- Notification history and tracking
- User preference management
- Bulk operations support
- Comprehensive monitoring and health checks

## Authentication
This API uses JWT Bearer tokens for authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

## Real-time Notifications
SignalR Hub is available at `/notificationHub` for real-time WebSocket connections.

## Rate Limiting
API requests are rate limited. Check response headers for current limits.
",
            Contact = new() 
            { 
                Name = "Notification Service Team",
                Email = "support@example.com",
                Url = new Uri("https://github.com/rickymta/isra-notification-service")
            },
            License = new()
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

        // Add JWT Authentication
        c.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
        });

        c.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new()
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Configure response types
        c.OperationFilter<SwaggerResponseTypesOperationFilter>();
        
        // Include XML comments
        var xmlFiles = new[]
        {
            $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml",
            "NotificationService.Domain.xml",
            "NotificationService.Application.xml"
        };

        foreach (var xmlFile in xmlFiles)
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        }

        // Group endpoints by tags
        c.TagActionsBy(api => 
        {
            var controllerName = api.ActionDescriptor.RouteValues["controller"];
            return controllerName switch
            {
                "Notifications" => new[] { "Standard Notifications" },
                "InAppNotifications" => new[] { "In-App & Real-time Notifications" },
                "Templates" => new[] { "Notification Templates" },
                _ => new[] { controllerName ?? "General" }
            };
        });

        // Custom schema filters for better documentation
        c.SchemaFilter<ExampleSchemaFilter>();
        c.DocumentFilter<CustomDocumentFilter>();
    });

    // Custom Swagger operation filter
    builder.Services.AddTransient<SwaggerResponseTypesOperationFilter>();

    // Add notification services
    builder.Services.AddNotificationServices(builder.Configuration);

    // Add SignalR for real-time notifications
    builder.Services.AddSignalRNotifications(builder.Configuration);
    builder.Services.AddSignalRJsonProtocol(builder.Configuration);
    builder.Services.AddSignalRCors(builder.Configuration);

    // Add OpenTelemetry distributed tracing
    builder.Services.AddOpenTelemetryTracing(builder.Configuration);

    // Add health checks
    builder.Services.AddHealthChecks();

    // Add Prometheus metrics
    builder.Services.AddSingleton(Metrics.DefaultRegistry);

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
        
        // Add SignalR specific CORS policy
        options.AddPolicy("SignalRCorsPolicy", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("SignalR:AllowedOrigins").Get<string[]>() ?? new[] { "*" })
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "Notification Service API Documentation";
            c.EnableDeepLinking();
            c.DisplayRequestDuration();
            c.EnableTryItOutByDefault();
            c.EnableFilter();
            c.ShowExtensions();
            c.EnableValidator();
            c.SupportedSubmitMethods(Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get, 
                                   Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post, 
                                   Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put, 
                                   Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete);
            
            // Add custom CSS for better styling
            c.InjectStylesheet("/swagger/custom.css");
            
            // Custom JavaScript for enhanced functionality
            c.InjectJavascript("/swagger/custom.js");
        });
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles(); // Enable static files for custom Swagger assets
    app.UseCors();

    // Add Prometheus metrics middleware
    app.UseHttpMetrics();
    app.MapMetrics();

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    // Map SignalR Hub
    app.MapHub<NotificationService.Infrastructure.SignalR.NotificationHub>("/notificationHub")
        .RequireCors("SignalRCorsPolicy");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Simple HostEnvironment implementation
public class HostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "NotificationService.Api";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
}
