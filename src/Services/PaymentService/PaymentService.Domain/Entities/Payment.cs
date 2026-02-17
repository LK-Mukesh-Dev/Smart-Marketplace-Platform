using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? TransactionId { get; private set; }
    public string? GatewayResponse { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private Payment() { }

    public Payment(Guid orderId, decimal amount)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty", nameof(orderId));
        
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Id = Guid.NewGuid();
        OrderId = orderId;
        Amount = amount;
        Status = PaymentStatus.Initiated;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkSuccess(string transactionId, string? gatewayResponse = null)
    {
        if (Status != PaymentStatus.Initiated && Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot mark payment as success from {Status} status");

        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID is required", nameof(transactionId));

        Status = PaymentStatus.Success;
        TransactionId = transactionId;
        GatewayResponse = gatewayResponse;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        if (Status == PaymentStatus.Success)
            throw new InvalidOperationException("Cannot mark successful payment as failed");

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        FailedAt = DateTime.UtcNow;
    }

    public void MarkProcessing()
    {
        if (Status != PaymentStatus.Initiated)
            throw new InvalidOperationException($"Cannot mark payment as processing from {Status} status");

        Status = PaymentStatus.Processing;
    }

    public bool CanRetry()
    {
        return Status == PaymentStatus.Failed || Status == PaymentStatus.Initiated;
    }
}
