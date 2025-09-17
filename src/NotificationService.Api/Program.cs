using NotificationService.Infrastructure.Extensions;
using NotificationService.Infrastructure.Logging;
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
            Description = "A microservice for sending notifications via Email, SMS, and Push channels"
        });
        
        // Include XML comments
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Add notification services
    builder.Services.AddNotificationServices(builder.Configuration);

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
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors();

    // Add Prometheus metrics middleware
    app.UseHttpMetrics();
    app.MapMetrics();

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

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
