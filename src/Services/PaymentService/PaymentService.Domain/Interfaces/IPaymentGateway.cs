namespace PaymentService.Domain.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(Guid orderId, decimal amount, CancellationToken cancellationToken = default);
}

public record PaymentGatewayResult
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public string? ErrorMessage { get; init; }
    public string? GatewayResponse { get; init; }
}
