using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Factories;

public class NotificationChannelFactory : INotificationChannelFactory
{
    private readonly ILogger<EmailChannel> _emailLogger;
    private readonly ILogger<SmsChannel> _smsLogger;
    private readonly ILogger<PushChannel> _pushLogger;

    public NotificationChannelFactory(
        ILogger<EmailChannel> emailLogger,
        ILogger<SmsChannel> smsLogger,
        ILogger<PushChannel> pushLogger)
    {
        _emailLogger = emailLogger ?? throw new ArgumentNullException(nameof(emailLogger));
        _smsLogger = smsLogger ?? throw new ArgumentNullException(nameof(smsLogger));
        _pushLogger = pushLogger ?? throw new ArgumentNullException(nameof(pushLogger));
    }

    public INotificationChannel GetChannel(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => new Channels.EmailChannel(_emailLogger),
            NotificationChannel.Sms => new Channels.SmsChannel(_smsLogger),
            NotificationChannel.Push => new Channels.PushChannel(_pushLogger),
            _ => throw new ArgumentException($"Unknown notification channel: {channel}", nameof(channel))
        };
    }
}
