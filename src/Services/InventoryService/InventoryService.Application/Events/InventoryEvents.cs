namespace InventoryService.Application.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class OrderItemEvent
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class PaymentFailedEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
}

public class StockReservedEvent
{
    public Guid ReservationId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
}

public class StockReservationFailedEvent
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}
