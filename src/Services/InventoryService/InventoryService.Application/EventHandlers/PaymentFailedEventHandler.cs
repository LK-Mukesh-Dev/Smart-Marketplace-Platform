using InventoryService.Application.Events;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.EventHandlers;

public class PaymentFailedEventHandler
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockReservationRepository _reservationRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(
        IInventoryRepository inventoryRepository,
        IStockReservationRepository reservationRepository,
        IStockMovementRepository movementRepository,
        IDistributedLock distributedLock,
        ILogger<PaymentFailedEventHandler> logger)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _distributedLock = distributedLock ?? throw new ArgumentNullException(nameof(distributedLock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HandleAsync(PaymentFailedEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing PaymentFailed event for Order: {OrderId}", paymentEvent.OrderId);

        // Get reservation for the order
        var reservation = await _reservationRepository.GetByOrderIdAsync(paymentEvent.OrderId, cancellationToken);

        if (reservation == null)
        {
            _logger.LogWarning("No reservation found for Order: {OrderId}", paymentEvent.OrderId);
            return false;
        }

        var lockKey = $"inventory:lock:{reservation.ProductId}";
        var lockAcquired = false;

        try
        {
            // Acquire lock
            lockAcquired = await _distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30), cancellationToken);

            if (!lockAcquired)
            {
                _logger.LogWarning("Failed to acquire lock for Product: {ProductId}", reservation.ProductId);
                return false;
            }

            // Get inventory item
            var inventoryItem = await _inventoryRepository.GetByProductIdAsync(reservation.ProductId, cancellationToken);

            if (inventoryItem == null)
            {
                _logger.LogError("Inventory item not found for Product: {ProductId}", reservation.ProductId);
                return false;
            }

            // Release the reservation
            var quantityBefore = inventoryItem.QuantityAvailable;
            inventoryItem.ReleaseReservation(reservation.Quantity);
            await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);

            // Update reservation status
            reservation.Release(paymentEvent.Reason);
            await _reservationRepository.UpdateAsync(reservation, cancellationToken);

            // Log stock movement
            var movement = new StockMovement(
                reservation.ProductId,
                StockMovementType.Released,
                reservation.Quantity,
                quantityBefore,
                inventoryItem.QuantityAvailable,
                paymentEvent.OrderId.ToString(),
                $"Released due to payment failure: {paymentEvent.Reason}");
            await _movementRepository.CreateAsync(movement, cancellationToken);

            _logger.LogInformation(
                "Stock reservation released. Product: {ProductId}, Quantity: {Quantity}, Order: {OrderId}",
                reservation.ProductId, reservation.Quantity, paymentEvent.OrderId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing stock reservation for Order: {OrderId}", paymentEvent.OrderId);
            return false;
        }
        finally
        {
            if (lockAcquired)
            {
                await _distributedLock.ReleaseLockAsync(lockKey, cancellationToken);
            }
        }
    }
}
