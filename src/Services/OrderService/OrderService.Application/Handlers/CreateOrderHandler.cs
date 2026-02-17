using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Handlers;

public class CreateOrderHandler
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<OrderDto> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var shippingAddress = new Address(
            command.ShippingAddress.Street,
            command.ShippingAddress.City,
            command.ShippingAddress.State,
            command.ShippingAddress.Country,
            command.ShippingAddress.PostalCode
        );

        var orderItems = command.Items.Select(item => new OrderItem(
            item.ProductId,
            item.ProductName,
            item.Quantity,
            new Money(item.UnitPrice, item.Currency)
        )).ToList();

        var shippingCost = new Money(command.ShippingCost);
        var tax = new Money(command.Tax);

        var order = new Order(
            command.UserId,
            shippingAddress,
            orderItems,
            shippingCost,
            tax
        );

        var createdOrder = await _orderRepository.CreateAsync(order, cancellationToken);

        return MapToDto(createdOrder);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            ShippingAddress = new AddressDto
            {
                Street = order.ShippingAddress.Street,
                City = order.ShippingAddress.City,
                State = order.ShippingAddress.State,
                Country = order.ShippingAddress.Country,
                PostalCode = order.ShippingAddress.PostalCode
            },
            TotalAmount = order.TotalAmount.Amount,
            ShippingCost = order.ShippingCost.Amount,
            Tax = order.Tax.Amount,
            GrandTotal = order.GrandTotal.Amount,
            Currency = order.TotalAmount.Currency,
            Notes = order.Notes,
            Items = order.Items.Select(item => new OrderItemResponseDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice.Amount,
                TotalPrice = item.TotalPrice.Amount,
                Currency = item.UnitPrice.Currency
            }).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt
        };
    }
}
