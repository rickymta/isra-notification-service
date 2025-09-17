using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of message consumer
/// </summary>
public class RabbitMqMessageConsumer : IMessageConsumer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqMessageConsumer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IServiceProvider _serviceProvider;
    private EventingBasicConsumer? _consumer;
    private string? _consumerTag;

    public RabbitMqMessageConsumer(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqMessageConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _settings = settings.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
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
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Set QoS to process one message at a time
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += OnMessageReceived;
        
        _consumerTag = _channel.BasicConsume(
            queue: _settings.NotificationQueue,
            autoAck: false,
            consumer: _consumer);

        _logger.LogInformation("Started consuming messages from queue {Queue}", _settings.NotificationQueue);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_consumerTag))
        {
            _channel.BasicCancel(_consumerTag);
            _consumerTag = null;
        }

        _logger.LogInformation("Stopped consuming messages from queue {Queue}", _settings.NotificationQueue);
        
        return Task.CompletedTask;
    }

    private async void OnMessageReceived(object? sender, BasicDeliverEventArgs e)
    {
        var deliveryTag = e.DeliveryTag;
        
        try
        {
            var messageBody = Encoding.UTF8.GetString(e.Body.ToArray());
            _logger.LogDebug("Received message: {MessageBody}", messageBody);

            var notificationRequest = JsonSerializer.Deserialize<NotificationRequest>(messageBody, _jsonOptions);
            
            if (notificationRequest == null)
            {
                _logger.LogError("Failed to deserialize notification request from message");
                _channel.BasicNack(deliveryTag, multiple: false, requeue: false);
                return;
            }

            _logger.LogInformation("Processing notification request {RequestId}", notificationRequest.Id);

            // Process the notification using the service provider
            using var scope = _serviceProvider.CreateScope();
            var notificationProcessor = scope.ServiceProvider.GetRequiredService<INotificationProcessor>();
            
            await notificationProcessor.ProcessNotificationAsync(notificationRequest);

            // Acknowledge the message
            _channel.BasicAck(deliveryTag, multiple: false);
            
            _logger.LogInformation("Successfully processed notification request {RequestId}", notificationRequest.Id);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message, rejecting without requeue");
            _channel.BasicNack(deliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process notification, rejecting with requeue for retry");
            
            // Check if this is a retry by examining the headers
            var retryCount = GetRetryCount(e.BasicProperties);
            
            if (retryCount < _settings.MaxRetryAttempts)
            {
                // Requeue for retry
                _channel.BasicNack(deliveryTag, multiple: false, requeue: true);
            }
            else
            {
                _logger.LogError("Maximum retry attempts reached for message, sending to dead letter queue");
                _channel.BasicNack(deliveryTag, multiple: false, requeue: false);
            }
        }
    }

    private int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers != null && 
            properties.Headers.TryGetValue("x-retry-count", out var retryCountObj) &&
            retryCountObj is byte[] retryCountBytes)
        {
            if (int.TryParse(Encoding.UTF8.GetString(retryCountBytes), out var retryCount))
            {
                return retryCount;
            }
        }
        
        return 0;
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}