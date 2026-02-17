using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Channels;

public class SmsChannel : INotificationChannel
{
    private readonly ISmsProvider _smsProvider;
    private readonly ILogger<SmsChannel> _logger;

    public SmsChannel(ISmsProvider smsProvider, ILogger<SmsChannel> logger)
    {
        _smsProvider = smsProvider ?? throw new ArgumentNullException(nameof(smsProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending SMS via {Provider} to: {Recipient}",
            _smsProvider.ProviderName, request.Recipient);

        try
        {
            var result = await _smsProvider.SendSmsAsync(
                request.Recipient,
                request.Body,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("SMS sent successfully via {Provider}. MessageId: {MessageId}",
                    _smsProvider.ProviderName, result.MessageId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send SMS via {Provider}. Error: {Error}",
                    _smsProvider.ProviderName, result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via {Provider} to: {Recipient}",
                _smsProvider.ProviderName, request.Recipient);
            return false;
        }
    }
}
