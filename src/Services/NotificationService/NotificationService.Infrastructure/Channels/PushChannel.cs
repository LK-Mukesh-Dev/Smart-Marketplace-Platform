using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Channels;

public class PushChannel : INotificationChannel
{
    private readonly ILogger<PushChannel> _logger;

    public PushChannel(ILogger<PushChannel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending push notification to: {Recipient}", request.Recipient);

        try
        {
            // Simulate push notification delay
            await Task.Delay(50, cancellationToken);

            // Simulate 95% success rate
            var success = Random.Shared.Next(1, 21) > 1;

            if (success)
            {
                _logger.LogInformation("Push notification sent successfully to: {Recipient}", request.Recipient);
            }
            else
            {
                _logger.LogWarning("Failed to send push notification to: {Recipient}", request.Recipient);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to: {Recipient}", request.Recipient);
            return false;
        }
    }
}
