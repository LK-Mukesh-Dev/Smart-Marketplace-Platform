using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Domain.Interfaces;

public interface INotificationChannelFactory
{
    INotificationChannel GetChannel(NotificationChannel channel);
}
