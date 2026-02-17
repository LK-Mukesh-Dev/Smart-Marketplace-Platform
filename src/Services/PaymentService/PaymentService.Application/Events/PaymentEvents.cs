namespace PaymentService.Application.Events;

public record InventoryReservedEvent
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public DateTime Timestamp { get; init; }
}

public record PaymentCompletedEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime Timestamp { get; init; }
}

public record PaymentFailedEvent
{
    public Guid OrderId { get; init; }
    public Guid? PaymentId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
