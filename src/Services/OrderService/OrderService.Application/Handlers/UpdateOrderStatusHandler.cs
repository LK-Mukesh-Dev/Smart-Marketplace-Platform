using OrderService.Application.Commands;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class UpdateOrderStatusHandler
{
    private readonly IOrderRepository _orderRepository;

    public UpdateOrderStatusHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<bool> HandleAsync(UpdateOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        
        if (order == null)
            return false;

        switch (command.NewStatus.ToLower())
        {
            case "confirmed":
                order.ConfirmOrder();
                break;
            case "processing":
                order.StartProcessing();
                break;
            case "shipped":
                order.Ship();
                break;
            case "delivered":
                order.Deliver();
                break;
            case "cancelled":
                order.Cancel(command.Reason ?? "Cancelled by user");
                break;
            default:
                throw new InvalidOperationException($"Invalid status: {command.NewStatus}");
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);
        return true;
    }
}
