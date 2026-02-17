using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Services;

public class NotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationChannelFactory _channelFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationChannelFactory channelFactory,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _channelFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool Success, string Message)> ProcessNotificationAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing notification: {NotificationId}, Channel: {Channel}, EventType: {EventType}",
            request.Id, request.Channel, request.EventType);

        try
        {
            // Check idempotency
            var existingNotification = await _notificationRepository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey!, cancellationToken);

            if (existingNotification != null)
            {
                _logger.LogInformation("Notification already processed. IdempotencyKey: {IdempotencyKey}",
                    request.IdempotencyKey);
                return (true, "Notification already processed");
            }

            // Save notification request
            await _notificationRepository.CreateAsync(request, cancellationToken);

            // Get appropriate channel
            var channel = _channelFactory.GetChannel(request.Channel);

            // Send notification
            var success = await channel.SendAsync(request, cancellationToken);

            if (success)
            {
                request.MarkAsSent();
                _logger.LogInformation("Notification sent successfully: {NotificationId}", request.Id);
            }
            else
            {
                request.MarkAsFailed("Failed to send notification");
                _logger.LogWarning("Notification failed: {NotificationId}", request.Id);
            }

            await _notificationRepository.UpdateAsync(request, cancellationToken);

            return (success, success ? "Notification sent" : "Failed to send notification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification: {NotificationId}", request.Id);
            
            try
            {
                request.MarkAsFailed(ex.Message);
                await _notificationRepository.UpdateAsync(request, cancellationToken);
            }
            catch
            {
                // Swallow update errors
            }

            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RetryFailedNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);

        if (notification == null)
        {
            _logger.LogWarning("Notification not found: {NotificationId}", notificationId);
            return (false, "Notification not found");
        }

        if (!notification.CanRetry())
        {
            _logger.LogWarning("Notification cannot be retried: {NotificationId}, Status: {Status}, RetryCount: {RetryCount}",
                notificationId, notification.Status, notification.RetryCount);
            return (false, "Notification cannot be retried");
        }

        try
        {
            notification.IncrementRetryCount();
            await _notificationRepository.UpdateAsync(notification, cancellationToken);

            var channel = _channelFactory.GetChannel(notification.Channel);
            var success = await channel.SendAsync(notification, cancellationToken);

            if (success)
            {
                notification.MarkAsSent();
                _logger.LogInformation("Notification retry successful: {NotificationId}", notificationId);
            }
            else
            {
                notification.MarkAsFailed("Retry failed");
                _logger.LogWarning("Notification retry failed: {NotificationId}", notificationId);
            }

            await _notificationRepository.UpdateAsync(notification, cancellationToken);

            return (success, success ? "Retry successful" : "Retry failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying notification: {NotificationId}", notificationId);
            return (false, $"Retry error: {ex.Message}");
        }
    }
}
