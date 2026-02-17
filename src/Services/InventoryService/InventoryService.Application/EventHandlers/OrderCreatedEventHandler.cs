using InventoryService.Application.Events;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.EventHandlers;

public class OrderCreatedEventHandler
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockReservationRepository _reservationRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(
        IInventoryRepository inventoryRepository,
        IStockReservationRepository reservationRepository,
        IStockMovementRepository movementRepository,
        IDistributedLock distributedLock,
        ILogger<OrderCreatedEventHandler> logger)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _distributedLock = distributedLock ?? throw new ArgumentNullException(nameof(distributedLock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool Success, string Message)> HandleAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing OrderCreated event for Order: {OrderId}", orderEvent.OrderId);

        foreach (var item in orderEvent.Items)
        {
            var lockKey = $"inventory:lock:{item.ProductId}";
            var lockAcquired = false;

            try
            {
                // Acquire distributed lock to prevent race conditions
                lockAcquired = await _distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30), cancellationToken);

                if (!lockAcquired)
                {
                    _logger.LogWarning("Failed to acquire lock for Product: {ProductId}", item.ProductId);
                    return (false, $"Failed to acquire lock for product {item.ProductId}");
                }

                // Get inventory item
                var inventoryItem = await _inventoryRepository.GetByProductIdAsync(item.ProductId, cancellationToken);

                if (inventoryItem == null)
                {
                    _logger.LogWarning("Product not found in inventory: {ProductId}", item.ProductId);
                    return (false, $"Product {item.ProductId} not found in inventory");
                }

                // Check if stock is available
                if (!inventoryItem.CanReserve(item.Quantity))
                {
                    _logger.LogWarning(
                        "Insufficient stock for Product: {ProductId}. Available: {Available}, Requested: {Requested}",
                        item.ProductId, inventoryItem.QuantityAvailable, item.Quantity);

                    return (false, $"Insufficient stock for product {item.ProductId}. Available: {inventoryItem.QuantityAvailable}, Requested: {item.Quantity}");
                }

                // Reserve stock
                var quantityBefore = inventoryItem.QuantityAvailable;
                inventoryItem.Reserve(item.Quantity);
                await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);

                // Create reservation record
                var reservation = new StockReservation(item.ProductId, orderEvent.OrderId, item.Quantity);
                await _reservationRepository.CreateAsync(reservation, cancellationToken);

                // Log stock movement
                var movement = new StockMovement(
                    item.ProductId,
                    StockMovementType.Reserved,
                    item.Quantity,
                    quantityBefore,
                    inventoryItem.QuantityAvailable,
                    orderEvent.OrderId.ToString(),
                    $"Reserved for order {orderEvent.OrderId}");
                await _movementRepository.CreateAsync(movement, cancellationToken);

                _logger.LogInformation(
                    "Stock reserved successfully. Product: {ProductId}, Quantity: {Quantity}, Reservation: {ReservationId}",
                    item.ProductId, item.Quantity, reservation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving stock for Product: {ProductId}, Order: {OrderId}", item.ProductId, orderEvent.OrderId);
                return (false, $"Error reserving stock: {ex.Message}");
            }
            finally
            {
                if (lockAcquired)
                {
                    await _distributedLock.ReleaseLockAsync(lockKey, cancellationToken);
                }
            }
        }

        _logger.LogInformation("All stock reserved successfully for Order: {OrderId}", orderEvent.OrderId);
        return (true, "Stock reserved successfully");
    }
}
