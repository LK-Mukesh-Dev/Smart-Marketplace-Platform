using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace NotificationService.Infrastructure.SmsProviders;

/// <summary>
/// MSG91 Provider - Enterprise-grade SMS for India
/// Free: Trial credits on signup
/// Best for: OTP, transactional SMS, enterprise apps
/// Docs: https://docs.msg91.com/p/tf9GTextN/e/Oq3iKcLBL/MSG91
/// </summary>
public class MSG91SmsProvider : ISmsProvider
{
    private readonly MSG91Settings _settings;
    private readonly ILogger<MSG91SmsProvider> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "MSG91 (India)";

    public MSG91SmsProvider(
        MSG91Settings settings,
        ILogger<MSG91SmsProvider> logger,
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
            _logger.LogInformation("?? [MSG91] Sending SMS to: {PhoneNumber}", phoneNumber);

            // MSG91 API endpoint
            var url = "https://api.msg91.com/api/sendhttp.php";

            // Prepare query parameters
            var queryParams = new Dictionary<string, string>
            {
                { "authkey", _settings.AuthKey },
                { "mobiles", NormalizePhoneNumber(phoneNumber) },
                { "message", message },
                { "sender", _settings.SenderId },
                { "route", _settings.Route },
                { "country", "91" } // India
            };

            var queryString = string.Join("&", queryParams.Select(kvp => 
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var fullUrl = $"{url}?{queryString}";

            // Send request
            var response = await _httpClient.GetAsync(fullUrl, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // MSG91 returns JSON with message ID or error
                var result = JsonSerializer.Deserialize<MSG91Response>(responseContent);

                if (result?.Type == "success")
                {
                    _logger.LogInformation("? [MSG91] SMS sent successfully. MessageId: {MessageId}",
                        result.Message);

                    return new SmsProviderResult
                    {
                        Success = true,
                        MessageId = result.Message,
                        ProviderResponse = responseContent,
                        Cost = 0.0m // Free tier
                    };
                }
                else
                {
                    _logger.LogWarning("?? [MSG91] API returned error. Response: {Response}", responseContent);

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
                _logger.LogError("? [MSG91] Failed to send SMS. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return new SmsProviderResult
                {
                    Success = false,
                    ErrorMessage = $"MSG91 API error: {response.StatusCode}",
                    ProviderResponse = responseContent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? [MSG91] Exception while sending SMS");

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
        if (string.IsNullOrWhiteSpace(_settings.AuthKey))
            throw new InvalidOperationException("MSG91 AuthKey is required");
    }

    private string NormalizePhoneNumber(string phoneNumber)
    {
        // MSG91 expects 10-digit numbers without country code
        phoneNumber = phoneNumber.Replace("+91", "").Replace("+", "").Trim();

        if (phoneNumber.Length != 10)
        {
            _logger.LogWarning("?? [MSG91] Phone number length is {Length}, expected 10 digits", phoneNumber.Length);
        }

        return phoneNumber;
    }

    private class MSG91Response
    {
        public string? Type { get; set; } // "success" or "error"
        public string? Message { get; set; }
    }
}
