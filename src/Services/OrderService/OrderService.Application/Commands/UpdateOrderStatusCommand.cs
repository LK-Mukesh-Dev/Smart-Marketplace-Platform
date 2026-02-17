namespace OrderService.Application.Commands;

public record UpdateOrderStatusCommand
{
    public Guid OrderId { get; init; }
    public string NewStatus { get; init; } = string.Empty;
    public string? Reason { get; init; }
}
