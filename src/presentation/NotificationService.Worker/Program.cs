using NotificationService.Infrastructure.Extensions;
using NotificationService.Infrastructure.Logging;
using NotificationService.Worker;
using Serilog;

// Configure Serilog
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var hostEnvironment = new HostEnvironment { EnvironmentName = environment };
Log.Logger = SerilogConfiguration.ConfigureSerilog(configuration, hostEnvironment).CreateLogger();

try
{
    Log.Information("Starting NotificationService.Worker");

    var builder = Host.CreateApplicationBuilder(args);

    // Replace default logging with Serilog
    builder.Services.AddSerilog();

    // Add notification services
    builder.Services.AddNotificationServices(builder.Configuration);

    // Add the background worker
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<NotificationWorker>();

    var host = builder.Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService.Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Simple HostEnvironment implementation
public class HostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "NotificationService.Worker";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
}
