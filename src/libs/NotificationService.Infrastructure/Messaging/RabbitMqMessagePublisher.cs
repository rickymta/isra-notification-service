using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Extensions;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of message publisher with optimized connection pooling
/// </summary>
public class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IRabbitMqConnectionPool _connectionPool;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _infrastructureInitialized;
    private readonly object _initLock = new();

    public RabbitMqMessagePublisher(
        IRabbitMqConnectionPool connectionPool,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqMessagePublisher> logger)
    {
        _connectionPool = connectionPool;
        _settings = settings.Value;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Publishes a notification request to the queue
    /// </summary>
    public async Task PublishNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        await EnsureInfrastructureAsync();

        var channel = await _connectionPool.GetChannelAsync();
        
        try
        {
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Headers = new Dictionary<string, object>
            {
                ["type"] = "notification_request",
                ["version"] = "1.0"
            };

            var message = JsonSerializer.Serialize(request, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(
                exchange: _settings.Exchange,
                routingKey: _settings.RoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published notification request {RequestId} to queue", request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification request {RequestId}", request.Id);
            throw;
        }
        finally
        {
            _connectionPool.ReturnChannel(channel);
        }
    }

    public async Task PublishNotificationWithDelayAsync(NotificationRequest request, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        await EnsureInfrastructureAsync();

        var channel = await _connectionPool.GetChannelAsync();
        
        try
        {
            // For delayed publishing, we'll use TTL and dead letter exchange
            // This is a simplified implementation - in production, consider using RabbitMQ's delayed message plugin
            
            var properties = channel.CreateBasicProperties();
            properties.Expiration = ((int)delay.TotalMilliseconds).ToString();
            properties.Persistent = true;
            
            var delayQueueName = $"{_settings.NotificationQueue}_delay";
            
            // Declare delay queue with DLX pointing to main queue
            channel.QueueDeclare(
                queue: delayQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    {"x-dead-letter-exchange", _settings.Exchange},
                    {"x-dead-letter-routing-key", _settings.RoutingKey}
                });

            var messageBody = JsonSerializer.SerializeToUtf8Bytes(request, _jsonOptions);
            
            channel.BasicPublish(
                exchange: "",
                routingKey: delayQueueName,
                basicProperties: properties,
                body: messageBody);

            _logger.LogInformation("Published delayed notification {RequestId} with delay {Delay}",
                request.Id, delay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish delayed notification request {RequestId}", request.Id);
            throw;
        }
        finally
        {
            _connectionPool.ReturnChannel(channel);
        }
    }

    public async Task PublishRetryNotificationAsync(NotificationRequest request, int retryAttempt, CancellationToken cancellationToken = default)
    {
        // Calculate exponential backoff delay
        var delay = CalculateRetryDelay(retryAttempt);
        
        _logger.LogInformation("Publishing retry notification {RequestId}, attempt {RetryAttempt} with delay {Delay}",
            request.Id, retryAttempt, delay);
        
        await PublishNotificationWithDelayAsync(request, delay, cancellationToken);
    }

    private async Task PublishWithRetryAsync(NotificationRequest request, int attempt, CancellationToken cancellationToken)
    {
        var maxAttempts = _settings.MaxRetryAttempts;
        
        for (int i = attempt; i <= maxAttempts; i++)
        {
            try
            {
                await PublishNotificationAsync(request, cancellationToken);
                return; // Success, exit retry loop
            }
            catch (Exception ex) when (i < maxAttempts)
            {
                var delay = CalculateRetryDelay(i);
                _logger.LogWarning(ex, "Failed to publish notification {RequestId}, attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}",
                    request.Id, i, maxAttempts, delay);
                
                await Task.Delay(delay, cancellationToken);
            }
        }
        
        // If we get here, all retry attempts failed
        _logger.LogError("Failed to publish notification {RequestId} after {MaxAttempts} attempts", 
            request.Id, maxAttempts);
        throw new InvalidOperationException($"Failed to publish notification {request.Id} after {maxAttempts} attempts");
    }

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        // Exponential backoff with jitter
        var delay = Math.Min(
            _settings.InitialDelayMs * Math.Pow(2, attempt - 1),
            _settings.MaxDelayMs);
        
        // Add some jitter to prevent thundering herd
        var jitter = Random.Shared.Next(0, (int)(delay * 0.1));
        
        return TimeSpan.FromMilliseconds(delay + jitter);
    }

    private Task EnsureInfrastructureAsync()
    {
        if (_infrastructureInitialized)
            return Task.CompletedTask;

        lock (_initLock)
        {
            if (_infrastructureInitialized)
                return Task.CompletedTask;

            var channel = _connectionPool.GetChannel();
            try
            {
                DeclareInfrastructure(channel);
                _infrastructureInitialized = true;
                _logger.LogInformation("RabbitMQ infrastructure initialized successfully");
            }
            finally
            {
                _connectionPool.ReturnChannel(channel);
            }
        }
        
        return Task.CompletedTask;
    }

    private void DeclareInfrastructure(IModel channel)
    {
        // Declare exchange
        channel.ExchangeDeclare(
            exchange: _settings.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null);

        // Declare main notification queue
        channel.QueueDeclare(
            queue: _settings.NotificationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind queue to exchange
        channel.QueueBind(
            queue: _settings.NotificationQueue,
            exchange: _settings.Exchange,
            routingKey: _settings.RoutingKey,
            arguments: null);

        _logger.LogInformation("RabbitMQ infrastructure declared: Exchange={Exchange}, Queue={Queue}",
            _settings.Exchange, _settings.NotificationQueue);
    }

    public void Dispose()
    {
        // Connection pool will handle cleanup
        GC.SuppressFinalize(this);
    }
}