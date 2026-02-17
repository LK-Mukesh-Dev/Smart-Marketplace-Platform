using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Interfaces;

public interface ITemplateRepository
{
    Task<NotificationTemplate?> GetTemplateAsync(EventType eventType, NotificationChannel channel, CancellationToken cancellationToken = default);
    Task<NotificationTemplate> CreateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);
}
