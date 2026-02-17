namespace OrderService.Application.DTOs;

public record CreateOrderDto
{
    public Guid UserId { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public AddressDto ShippingAddress { get; init; } = null!;
    public decimal ShippingCost { get; init; }
    public decimal Tax { get; init; }
    public string? Notes { get; init; }
}

public record OrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = "USD";
}

public record AddressDto
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
}
