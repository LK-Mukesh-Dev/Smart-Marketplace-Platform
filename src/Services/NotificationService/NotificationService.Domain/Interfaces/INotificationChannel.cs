using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationChannel
{
    Task<bool> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
