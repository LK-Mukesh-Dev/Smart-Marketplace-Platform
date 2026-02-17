namespace NotificationService.Domain.Interfaces;

public interface ISmsProvider
{
    Task<SmsProviderResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    string ProviderName { get; }
}

public record SmsProviderResult
{
    public bool Success { get; init; }
    public string? MessageId { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ProviderResponse { get; init; }
    public decimal Cost { get; init; }
}
