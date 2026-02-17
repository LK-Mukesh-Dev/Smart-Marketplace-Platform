namespace OrderService.Application.DTOs;

public record OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public AddressDto ShippingAddress { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal Tax { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Notes { get; init; }
    public List<OrderItemResponseDto> Items { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
}

public record OrderItemResponseDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; } = "USD";
}
