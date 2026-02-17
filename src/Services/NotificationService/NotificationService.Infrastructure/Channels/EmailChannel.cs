using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Channels;

public class EmailChannel : INotificationChannel
{
    private readonly ILogger<EmailChannel> _logger;

    public EmailChannel(ILogger<EmailChannel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending email to: {Recipient}, Subject: {Subject}",
            request.Recipient, request.Subject);

        try
        {
            // Simulate email sending delay
            await Task.Delay(100, cancellationToken);

            // Simulate 90% success rate
            var success = Random.Shared.Next(1, 11) > 1;

            if (success)
            {
                _logger.LogInformation("Email sent successfully to: {Recipient}", request.Recipient);
            }
            else
            {
                _logger.LogWarning("Failed to send email to: {Recipient}", request.Recipient);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to: {Recipient}", request.Recipient);
            return false;
        }
    }
}
