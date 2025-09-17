using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Services.Strategies;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Factory for creating notification channel strategies
/// </summary>
public class NotificationChannelFactory : INotificationChannelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public NotificationChannelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public INotificationChannelStrategy GetStrategy(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => _serviceProvider.GetRequiredService<EmailChannelStrategy>(),
            NotificationChannel.Sms => _serviceProvider.GetRequiredService<SmsChannelStrategy>(),
            NotificationChannel.Push => _serviceProvider.GetRequiredService<PushChannelStrategy>(),
            _ => throw new NotSupportedException($"Notification channel {channel} is not supported")
        };
    }
}