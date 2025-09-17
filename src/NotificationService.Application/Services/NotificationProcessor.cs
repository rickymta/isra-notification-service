using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using System.Text.RegularExpressions;

namespace NotificationService.Application.Services;

/// <summary>
/// Main notification processor that orchestrates the notification sending process
/// </summary>
public class NotificationProcessor : INotificationProcessor
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationHistoryRepository _historyRepository;
    private readonly INotificationChannelFactory _channelFactory;
    private readonly ILogger<NotificationProcessor> _logger;

    public NotificationProcessor(
        INotificationTemplateRepository templateRepository,
        INotificationHistoryRepository historyRepository,
        INotificationChannelFactory channelFactory,
        ILogger<NotificationProcessor> logger)
    {
        _templateRepository = templateRepository;
        _historyRepository = historyRepository;
        _channelFactory = channelFactory;
        _logger = logger;
    }

    public async Task ProcessNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var history = await CreateNotificationHistoryAsync(request, cancellationToken);
        
        try
        {
            _logger.LogInformation("Processing notification request {RequestId} for channel {Channel}", 
                request.Id, request.Channel);

            // Get template
            var template = await GetTemplateAsync(request, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template not found: {request.TemplateId ?? request.TemplateName}");
            }

            // Update history with template info
            history.TemplateId = template.Id;
            history.TemplateName = template.Name;
            await _historyRepository.UpdateAsync(history, cancellationToken);

            // Build notification content
            var content = BuildNotificationContent(template, request.Variables);
            
            // Update history with content
            history.Content = content;
            history.Status = NotificationStatus.Processing;
            await _historyRepository.UpdateAsync(history, cancellationToken);

            // Get channel strategy
            var strategy = _channelFactory.GetStrategy(request.Channel);
            
            // Validate recipient
            if (!strategy.ValidateRecipient(request.Recipient))
            {
                throw new InvalidOperationException($"Invalid recipient for channel {request.Channel}");
            }

            // Send notification
            var result = await strategy.SendAsync(content, request.Recipient, cancellationToken);

            // Update history with result
            if (result.IsSuccess)
            {
                history.Status = NotificationStatus.Sent;
                history.SentAt = DateTime.UtcNow;
                history.ExternalMessageId = result.ExternalMessageId;
                history.Metadata = result.Metadata;
                
                _logger.LogInformation("Notification {RequestId} sent successfully via {Channel}", 
                    request.Id, request.Channel);
            }
            else
            {
                history.Status = NotificationStatus.Failed;
                history.ErrorMessage = result.ErrorMessage;
                history.Metadata = result.Metadata;
                
                _logger.LogError("Failed to send notification {RequestId} via {Channel}: {Error}", 
                    request.Id, request.Channel, result.ErrorMessage);
            }

            await _historyRepository.UpdateAsync(history, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while processing notification {RequestId}", request.Id);
            
            // Update history with error
            history.Status = NotificationStatus.Failed;
            history.ErrorMessage = ex.Message;
            await _historyRepository.UpdateAsync(history, cancellationToken);
            
            throw;
        }
    }

    private async Task<NotificationTemplate?> GetTemplateAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.TemplateId))
        {
            return await _templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);
        }
        
        if (!string.IsNullOrEmpty(request.TemplateName))
        {
            var language = request.Recipient.Language ?? "en";
            return await _templateRepository.GetByNameAndLanguageAsync(request.TemplateName, language, cancellationToken);
        }

        return null;
    }

    private static NotificationContent BuildNotificationContent(NotificationTemplate template, Dictionary<string, string> variables)
    {
        var content = new NotificationContent
        {
            Subject = ReplaceVariables(template.Subject, variables),
            Body = ReplaceVariables(template.Body, variables),
            Variables = new Dictionary<string, string>(variables)
        };

        return content;
    }

    private static string? ReplaceVariables(string? template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var result = template;
        
        foreach (var variable in variables)
        {
            var placeholder = $"{{{{{variable.Key}}}}}";
            result = result.Replace(placeholder, variable.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private async Task<NotificationHistory> CreateNotificationHistoryAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        var history = new NotificationHistory
        {
            Id = request.Id, // Use the same ID as the request
            TemplateId = request.TemplateId ?? string.Empty,
            TemplateName = request.TemplateName ?? string.Empty,
            Channel = request.Channel,
            Status = NotificationStatus.Pending,
            Recipient = request.Recipient,
            RetryCount = 0,
            MaxRetries = 3, // Default, can be configurable
            Metadata = new Dictionary<string, string>(request.Metadata)
        };

        return await _historyRepository.CreateAsync(history, cancellationToken);
    }
}