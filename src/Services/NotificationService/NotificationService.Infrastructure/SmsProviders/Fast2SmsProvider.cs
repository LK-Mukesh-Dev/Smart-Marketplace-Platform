using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace NotificationService.Infrastructure.SmsProviders;

/// <summary>
/// Fast2SMS Provider - Popular in India
/// Free: Signup credits (limited)
/// Best for: Indian phone numbers, OTP, promotional SMS
/// Docs: https://www.fast2sms.com/dashboard/dev-api
/// </summary>
public class Fast2SmsProvider : ISmsProvider
{
    private readonly Fast2SmsSettings _settings;
    private readonly ILogger<Fast2SmsProvider> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Fast2SMS (India)";

    public Fast2SmsProvider(
        Fast2SmsSettings settings,
        ILogger<Fast2SmsProvider> logger,
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
            _logger.LogInformation("?? [FAST2SMS] Sending SMS to: {PhoneNumber}", phoneNumber);

            // Fast2SMS API endpoint
            var url = "https://www.fast2sms.com/dev/bulkV2";

            // Prepare request
            var requestBody = new
            {
                route = _settings.Route,
                sender_id = _settings.SenderId,
                message = message,
                language = "english",
                flash = 0,
                numbers = NormalizePhoneNumber(phoneNumber)
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("authorization", _settings.ApiKey);

            // Send request
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<Fast2SmsResponse>(responseContent);

                if (result?.Return == true)
                {
                    _logger.LogInformation("? [FAST2SMS] SMS sent successfully. MessageId: {MessageId}", 
                        result.RequestId);

                    return new SmsProviderResult
                    {
                        Success = true,
                        MessageId = result.RequestId,
                        ProviderResponse = responseContent,
                        Cost = 0.0m // Free tier
                    };
                }
                else
                {
                    _logger.LogWarning("?? [FAST2SMS] API returned false. Response: {Response}", responseContent);

                    return new SmsProviderResult
                    {
                        Success = false,
                        ErrorMessage = result?.Message ?? "Unknown error",
                        ProviderResponse = responseContent
                    };
                }
            }
            else
            {
                _logger.LogError("? [FAST2SMS] Failed to send SMS. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return new SmsProviderResult
                {
                    Success = false,
                    ErrorMessage = $"Fast2SMS API error: {response.StatusCode}",
                    ProviderResponse = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? [FAST2SMS] Exception while sending SMS");

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
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Fast2SMS ApiKey is required");
    }

    private string NormalizePhoneNumber(string phoneNumber)
    {
        // Fast2SMS expects 10-digit Indian numbers without country code
        phoneNumber = phoneNumber.Replace("+91", "").Replace("+", "").Trim();
        
        if (phoneNumber.Length != 10)
        {
            _logger.LogWarning("?? [FAST2SMS] Phone number length is {Length}, expected 10 digits", phoneNumber.Length);
        }

        return phoneNumber;
    }

    private class Fast2SmsResponse
    {
        public bool Return { get; set; }
        public string? RequestId { get; set; }
        public string? Message { get; set; }
    }
}
