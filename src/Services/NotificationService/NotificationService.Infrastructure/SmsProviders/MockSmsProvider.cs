using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.SmsProviders;

/// <summary>
/// Mock SMS Provider - 100% Free, perfect for testing and development
/// Simulates real SMS behavior without actually sending messages
/// </summary>
public class MockSmsProvider : ISmsProvider
{
    private readonly ILogger<MockSmsProvider> _logger;

    public string ProviderName => "Mock SMS Provider";

    public MockSmsProvider(ILogger<MockSmsProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SmsProviderResult> SendSmsAsync(
        string phoneNumber, 
        string message, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("?? [MOCK SMS] Sending to: {PhoneNumber}", phoneNumber);
        _logger.LogInformation("?? [MOCK SMS] Message: {Message}", message);

        // Simulate network delay
        await Task.Delay(100, cancellationToken);

        // Simulate 95% success rate
        var success = Random.Shared.Next(1, 21) > 1;

        if (success)
        {
            var messageId = $"MOCK-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            
            _logger.LogInformation("? [MOCK SMS] Sent successfully. MessageId: {MessageId}", messageId);
            
            return new SmsProviderResult
            {
                Success = true,
                MessageId = messageId,
                ProviderResponse = $"Mock SMS sent to {phoneNumber}",
                Cost = 0.0m
            };
        }
        else
        {
            var errorMessage = "Network timeout (simulated)";
            
            _logger.LogWarning("? [MOCK SMS] Failed to send. Error: {Error}", errorMessage);
            
            return new SmsProviderResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ProviderResponse = "Mock failure",
                Cost = 0.0m
            };
        }
    }
}
