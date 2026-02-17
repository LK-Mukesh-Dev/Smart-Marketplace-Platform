using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationRepository
{
    Task<NotificationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationRequest?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<NotificationRequest> CreateAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task<List<NotificationRequest>> GetPendingNotificationsAsync(CancellationToken cancellationToken = default);
    Task<List<NotificationRequest>> GetFailedNotificationsAsync(CancellationToken cancellationToken = default);
}
