using NotificationService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Worker;

/// <summary>
/// Background worker service for processing notification queue
/// </summary>
public class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly IMessageConsumer _messageConsumer;

    public NotificationWorker(
        ILogger<NotificationWorker> logger,
        IMessageConsumer messageConsumer)
    {
        _logger = logger;
        _messageConsumer = messageConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker starting at: {time}", DateTimeOffset.Now);

        try
        {
            await _messageConsumer.StartAsync(stoppingToken);
            
            // Keep the worker running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("NotificationWorker was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationWorker encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NotificationWorker stopping at: {time}", DateTimeOffset.Now);
        
        await _messageConsumer.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}