using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Services.Strategies;

/// <summary>
/// Email notification channel strategy
/// </summary>
public class EmailChannelStrategy : INotificationChannelStrategy
{
    private readonly IEmailService _emailService;

    public EmailChannelStrategy(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationResult> SendAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        return await _emailService.SendEmailAsync(content, recipient, cancellationToken);
    }

    public bool ValidateRecipient(NotificationRecipient recipient)
    {
        return _emailService.ValidateRecipient(recipient);
    }
}

/// <summary>
/// SMS notification channel strategy
/// </summary>
public class SmsChannelStrategy : INotificationChannelStrategy
{
    private readonly ISmsService _smsService;

    public SmsChannelStrategy(ISmsService smsService)
    {
        _smsService = smsService;
    }

    public NotificationChannel Channel => NotificationChannel.Sms;

    public async Task<NotificationResult> SendAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        return await _smsService.SendSmsAsync(content, recipient, cancellationToken);
    }

    public bool ValidateRecipient(NotificationRecipient recipient)
    {
        return _smsService.ValidateRecipient(recipient);
    }
}

/// <summary>
/// Push notification channel strategy
/// </summary>
public class PushChannelStrategy : INotificationChannelStrategy
{
    private readonly IPushService _pushService;

    public PushChannelStrategy(IPushService pushService)
    {
        _pushService = pushService;
    }

    public NotificationChannel Channel => NotificationChannel.Push;

    public async Task<NotificationResult> SendAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        return await _pushService.SendPushAsync(content, recipient, cancellationToken);
    }

    public bool ValidateRecipient(NotificationRecipient recipient)
    {
        return _pushService.ValidateRecipient(recipient);
    }
}