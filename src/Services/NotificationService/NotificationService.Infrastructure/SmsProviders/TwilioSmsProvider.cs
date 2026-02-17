using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace NotificationService.Infrastructure.SmsProviders;

/// <summary>
/// Twilio SMS Provider - Best for international SMS
/// Trial: $15-$20 free credit (perfect for resume/portfolio)
/// Production: Pay-as-you-go pricing
/// Docs: https://www.twilio.com/docs/sms
/// </summary>
public class TwilioSmsProvider : ISmsProvider
{
    private readonly TwilioSettings _settings;
    private readonly ILogger<TwilioSmsProvider> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Twilio SMS";

    public TwilioSmsProvider(
        TwilioSettings settings,
        ILogger<TwilioSmsProvider> logger,
        HttpClient httpClient)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        ValidateSettings();
    }

    public async Task<SmsProviderResult> SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("?? [TWILIO] Sending SMS to: {PhoneNumber}", phoneNumber);

            // Twilio uses Basic Auth
            var authToken = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}"));

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

            // Twilio API endpoint
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Messages.json";

            // Prepare form data
            var formData = new Dictionary<string, string>
            {
                { "From", _settings.FromPhoneNumber },
                { "To", NormalizePhoneNumber(phoneNumber) },
                { "Body", message }
            };

            var content = new FormUrlEncodedContent(formData);

            // Send request
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TwilioResponse>(responseContent);
                
                _logger.LogInformation("? [TWILIO] SMS sent successfully. SID: {MessageSid}", result?.Sid);
                
                return new SmsProviderResult
                {
                    Success = true,
                    MessageId = result?.Sid,
                    ProviderResponse = responseContent,
                    Cost = result?.Price ?? 0.0m
                };
            }
            else
            {
                _logger.LogError("? [TWILIO] Failed to send SMS. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return new SmsProviderResult
                {
                    Success = false,
                    ErrorMessage = $"Twilio API error: {response.StatusCode}",
                    ProviderResponse = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? [TWILIO] Exception while sending SMS");
            
            return new SmsProviderResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProviderResponse = ex.ToString()
            };
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.AccountSid))
            throw new InvalidOperationException("Twilio AccountSid is required");

        if (string.IsNullOrWhiteSpace(_settings.AuthToken))
            throw new InvalidOperationException("Twilio AuthToken is required");

        if (string.IsNullOrWhiteSpace(_settings.FromPhoneNumber))
            throw new InvalidOperationException("Twilio FromPhoneNumber is required");
    }

    private string NormalizePhoneNumber(string phoneNumber)
    {
        // Ensure phone number starts with country code (e.g., +1 for US, +91 for India)
        if (!phoneNumber.StartsWith("+"))
        {
            // Default to +1 (US) if no country code - adjust as needed
            phoneNumber = "+1" + phoneNumber.TrimStart('1');
        }
        return phoneNumber;
    }

    private class TwilioResponse
    {
        public string? Sid { get; set; }
        public string? Status { get; set; }
        public decimal Price { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
