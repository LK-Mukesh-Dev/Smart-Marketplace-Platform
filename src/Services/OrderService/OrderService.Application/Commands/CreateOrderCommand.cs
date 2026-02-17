using OrderService.Application.DTOs;

namespace OrderService.Application.Commands;

public record CreateOrderCommand
{
    public Guid UserId { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public AddressDto ShippingAddress { get; init; } = null!;
    public decimal ShippingCost { get; init; }
    public decimal Tax { get; init; }
    public string? Notes { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
}
