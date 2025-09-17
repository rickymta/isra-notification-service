using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of message publisher
/// </summary>
public class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMqMessagePublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqMessagePublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Create connection
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange and queue
        DeclareInfrastructure();
    }

    public async Task PublishNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        await PublishWithRetryAsync(request, 0, cancellationToken);
    }

    public Task PublishNotificationWithDelayAsync(NotificationRequest request, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        // For delayed publishing, we'll use TTL and dead letter exchange
        // This is a simplified implementation - in production, consider using RabbitMQ's delayed message plugin
        
        var properties = _channel.CreateBasicProperties();
        properties.Expiration = ((int)delay.TotalMilliseconds).ToString();
        properties.Persistent = true;
        
        var delayQueueName = $"{_settings.NotificationQueue}_delay";
        
        // Declare delay queue with DLX pointing to main queue
        _channel.QueueDeclare(
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
        
        _channel.BasicPublish(
            exchange: "",
            routingKey: delayQueueName,
            basicProperties: properties,
            body: messageBody);

        _logger.LogInformation("Published delayed notification {RequestId} with delay {Delay}",
            request.Id, delay);
            
        return Task.CompletedTask;
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
        try
        {
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = request.Id;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            
            // Set priority based on notification priority
            properties.Priority = (byte)Math.Max(0, Math.Min(255, (6 - request.Priority) * 50));

            var messageBody = JsonSerializer.SerializeToUtf8Bytes(request, _jsonOptions);

            _channel.BasicPublish(
                exchange: _settings.Exchange,
                routingKey: _settings.RoutingKey,
                basicProperties: properties,
                body: messageBody);

            _logger.LogInformation("Published notification {RequestId} to queue {Queue}",
                request.Id, _settings.NotificationQueue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification {RequestId}, attempt {Attempt}",
                request.Id, attempt + 1);

            if (attempt < _settings.MaxRetryAttempts - 1)
            {
                var delay = CalculateRetryDelay(attempt);
                await Task.Delay(delay, cancellationToken);
                await PublishWithRetryAsync(request, attempt + 1, cancellationToken);
            }
            else
            {
                _logger.LogError("Failed to publish notification {RequestId} after {MaxAttempts} attempts",
                    request.Id, _settings.MaxRetryAttempts);
                throw;
            }
        }
    }

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        // Exponential backoff with jitter
        var baseDelay = _settings.InitialDelayMs;
        var exponentialDelay = (int)(baseDelay * Math.Pow(2, attempt));
        var maxDelay = _settings.MaxDelayMs;
        
        // Add jitter (±20%)
        var jitter = Random.Shared.NextDouble() * 0.4 - 0.2; // -20% to +20%
        var delayWithJitter = exponentialDelay * (1 + jitter);
        
        var finalDelay = Math.Min(delayWithJitter, maxDelay);
        
        return TimeSpan.FromMilliseconds(Math.Max(0, finalDelay));
    }

    private void DeclareInfrastructure()
    {
        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: _settings.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Declare main queue
        _channel.QueueDeclare(
            queue: _settings.NotificationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind queue to exchange
        _channel.QueueBind(
            queue: _settings.NotificationQueue,
            exchange: _settings.Exchange,
            routingKey: _settings.RoutingKey);

        _logger.LogInformation("RabbitMQ infrastructure declared: Exchange={Exchange}, Queue={Queue}",
            _settings.Exchange, _settings.NotificationQueue);
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}