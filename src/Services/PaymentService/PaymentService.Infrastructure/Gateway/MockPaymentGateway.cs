using PaymentService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace PaymentService.Infrastructure.Gateway;

public class MockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MockPaymentGateway> _logger;
    private readonly Random _random = new();

    public MockPaymentGateway(ILogger<MockPaymentGateway> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(
        Guid orderId, 
        decimal amount, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment through mock gateway. Order: {OrderId}, Amount: {Amount}", 
            orderId, amount);

        // Simulate gateway processing delay
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

        // Simulate 80% success rate
        var success = _random.Next(1, 11) > 2;

        if (success)
        {
            var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                GatewayResponse = $"Payment approved for amount {amount:C}"
            };
        }
        else
        {
            var errorMessages = new[]
            {
                "Insufficient funds",
                "Card declined",
                "Invalid card details",
                "Payment timeout",
                "Gateway temporarily unavailable"
            };

            var errorMessage = errorMessages[_random.Next(errorMessages.Length)];

            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                GatewayResponse = $"Payment declined: {errorMessage}"
            };
        }
    }
}
