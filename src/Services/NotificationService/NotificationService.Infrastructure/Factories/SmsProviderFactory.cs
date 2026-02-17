using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.SmsProviders;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Factories;

/// <summary>
/// Factory to create appropriate SMS provider based on configuration
/// Supports: Mock, Twilio, Fast2SMS, MSG91
/// </summary>
public class SmsProviderFactory
{
    private readonly SmsProviderSettings _settings;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public SmsProviderFactory(
        SmsProviderSettings settings,
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public ISmsProvider CreateProvider()
    {
        return _settings.Provider.ToLowerInvariant() switch
        {
            "mock" => CreateMockProvider(),
            "twilio" => CreateTwilioProvider(),
            "fast2sms" => CreateFast2SmsProvider(),
            "msg91" => CreateMSG91Provider(),
            _ => throw new InvalidOperationException($"Unknown SMS provider: {_settings.Provider}. " +
                "Supported providers: Mock, Twilio, Fast2SMS, MSG91")
        };
    }

    private ISmsProvider CreateMockProvider()
    {
        var logger = _loggerFactory.CreateLogger<MockSmsProvider>();
        return new MockSmsProvider(logger);
    }

    private ISmsProvider CreateTwilioProvider()
    {
        if (_settings.Twilio == null)
            throw new InvalidOperationException("Twilio settings are required when using Twilio provider");

        var logger = _loggerFactory.CreateLogger<TwilioSmsProvider>();
        var httpClient = _httpClientFactory.CreateClient("Twilio");
        
        return new TwilioSmsProvider(_settings.Twilio, logger, httpClient);
    }

    private ISmsProvider CreateFast2SmsProvider()
    {
        if (_settings.Fast2SMS == null)
            throw new InvalidOperationException("Fast2SMS settings are required when using Fast2SMS provider");

        var logger = _loggerFactory.CreateLogger<Fast2SmsProvider>();
        var httpClient = _httpClientFactory.CreateClient("Fast2SMS");
        
        return new Fast2SmsProvider(_settings.Fast2SMS, logger, httpClient);
    }

    private ISmsProvider CreateMSG91Provider()
    {
        if (_settings.MSG91 == null)
            throw new InvalidOperationException("MSG91 settings are required when using MSG91 provider");

        var logger = _loggerFactory.CreateLogger<MSG91SmsProvider>();
        var httpClient = _httpClientFactory.CreateClient("MSG91");
        
        return new MSG91SmsProvider(_settings.MSG91, logger, httpClient);
    }
}
