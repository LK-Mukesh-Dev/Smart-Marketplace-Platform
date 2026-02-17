using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Handlers;
using OrderService.Domain.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderHandler _createOrderHandler;
    private readonly UpdateOrderStatusHandler _updateOrderStatusHandler;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        CreateOrderHandler createOrderHandler,
        UpdateOrderStatusHandler updateOrderStatusHandler,
        IOrderRepository orderRepository,
        ILogger<OrdersController> logger)
    {
        _createOrderHandler = createOrderHandler ?? throw new ArgumentNullException(nameof(createOrderHandler));
        _updateOrderStatusHandler = updateOrderStatusHandler ?? throw new ArgumentNullException(nameof(updateOrderStatusHandler));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto, [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey)
    {
        try
        {
            var command = new CreateOrderCommand
            {
                UserId = dto.UserId,
                Items = dto.Items,
                ShippingAddress = dto.ShippingAddress,
                ShippingCost = dto.ShippingCost,
                Tax = dto.Tax,
                Notes = dto.Notes,
                IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString()
            };

            var result = await _createOrderHandler.HandleAsync(command);
            
            _logger.LogInformation("Order created successfully: {OrderId}", result.Id);
            
            return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { error = "Failed to create order", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id);
            
            if (order == null)
                return NotFound(new { error = "Order not found" });

            var dto = new OrderDto
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

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new { error = "Failed to retrieve order" });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<OrderDto>>> GetUserOrders(Guid userId)
    {
        try
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId);
            
            var dtos = orders.Select(order => new OrderDto
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
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve orders" });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            var command = new UpdateOrderStatusCommand
            {
                OrderId = id,
                NewStatus = dto.NewStatus,
                Reason = dto.Reason
            };

            var success = await _updateOrderStatusHandler.HandleAsync(command);
            
            if (!success)
                return NotFound(new { error = "Order not found" });

            _logger.LogInformation("Order status updated: {OrderId} to {NewStatus}", id, dto.NewStatus);
            
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for {OrderId}", id);
            return StatusCode(500, new { error = "Failed to update order status" });
        }
    }
}

public record UpdateOrderStatusDto
{
    public string NewStatus { get; init; } = string.Empty;
    public string? Reason { get; init; }
}
