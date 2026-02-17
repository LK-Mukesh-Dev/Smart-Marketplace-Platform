using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.DTOs;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Interfaces;

namespace InventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryRepository inventoryRepository,
        IStockMovementRepository movementRepository,
        IDistributedLock distributedLock,
        ILogger<InventoryController> logger)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _distributedLock = distributedLock ?? throw new ArgumentNullException(nameof(distributedLock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<ActionResult<InventoryItemDto>> CreateInventoryItem([FromBody] CreateInventoryItemDto dto)
    {
        try
        {
            if (await _inventoryRepository.ExistsAsync(dto.ProductId))
            {
                return Conflict(new { error = "Inventory item already exists for this product" });
            }

            var item = new InventoryItem(
                dto.ProductId,
                dto.ProductName,
                dto.Sku,
                dto.InitialQuantity,
                dto.ReorderLevel,
                dto.MaxStockLevel);

            var created = await _inventoryRepository.CreateAsync(item);

            // Log initial stock movement
            var movement = new StockMovement(
                item.ProductId,
                StockMovementType.StockIn,
                dto.InitialQuantity,
                0,
                dto.InitialQuantity,
                "INITIAL",
                "Initial stock creation");
            await _movementRepository.CreateAsync(movement);

            var result = MapToDto(created);
            return CreatedAtAction(nameof(GetByProductId), new { productId = created.ProductId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory item");
            return StatusCode(500, new { error = "Failed to create inventory item", details = ex.Message });
        }
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<InventoryItemDto>> GetByProductId(Guid productId)
    {
        try
        {
            var item = await _inventoryRepository.GetByProductIdAsync(productId);
            
            if (item == null)
                return NotFound(new { error = "Inventory item not found" });

            return Ok(MapToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to retrieve inventory item" });
        }
    }

    [HttpGet("product/{productId}/check")]
    public async Task<ActionResult<StockCheckDto>> CheckStock(Guid productId, [FromQuery] int quantity = 1)
    {
        try
        {
            var item = await _inventoryRepository.GetByProductIdAsync(productId);
            
            if (item == null)
                return NotFound(new { error = "Product not found in inventory" });

            var result = new StockCheckDto
            {
                ProductId = productId,
                QuantityAvailable = item.QuantityAvailable,
                QuantityReserved = item.QuantityReserved,
                CanFulfill = item.CanReserve(quantity)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock for product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to check stock" });
        }
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetLowStockItems()
    {
        try
        {
            var items = await _inventoryRepository.GetLowStockItemsAsync();
            var dtos = items.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock items");
            return StatusCode(500, new { error = "Failed to retrieve low stock items" });
        }
    }

    [HttpPost("product/{productId}/add-stock")]
    public async Task<ActionResult> AddStock(Guid productId, [FromBody] UpdateStockDto dto)
    {
        var lockKey = $"inventory:lock:{productId}";
        var lockAcquired = false;

        try
        {
            lockAcquired = await _distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30));

            if (!lockAcquired)
                return StatusCode(423, new { error = "Failed to acquire lock. Please try again" });

            var item = await _inventoryRepository.GetByProductIdAsync(productId);
            
            if (item == null)
                return NotFound(new { error = "Product not found in inventory" });

            var quantityBefore = item.QuantityAvailable;
            item.AddStock(dto.Quantity);
            await _inventoryRepository.UpdateAsync(item);

            // Log stock movement
            var movement = new StockMovement(
                productId,
                StockMovementType.StockIn,
                dto.Quantity,
                quantityBefore,
                item.QuantityAvailable,
                null,
                dto.Notes);
            await _movementRepository.CreateAsync(movement);

            _logger.LogInformation("Stock added: Product {ProductId}, Quantity {Quantity}", productId, dto.Quantity);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding stock for product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to add stock" });
        }
        finally
        {
            if (lockAcquired)
            {
                await _distributedLock.ReleaseLockAsync(lockKey);
            }
        }
    }

    [HttpPost("product/{productId}/remove-stock")]
    public async Task<ActionResult> RemoveStock(Guid productId, [FromBody] UpdateStockDto dto)
    {
        var lockKey = $"inventory:lock:{productId}";
        var lockAcquired = false;

        try
        {
            lockAcquired = await _distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30));

            if (!lockAcquired)
                return StatusCode(423, new { error = "Failed to acquire lock. Please try again" });

            var item = await _inventoryRepository.GetByProductIdAsync(productId);
            
            if (item == null)
                return NotFound(new { error = "Product not found in inventory" });

            var quantityBefore = item.QuantityAvailable;
            item.RemoveStock(dto.Quantity);
            await _inventoryRepository.UpdateAsync(item);

            // Log stock movement
            var movement = new StockMovement(
                productId,
                StockMovementType.StockOut,
                dto.Quantity,
                quantityBefore,
                item.QuantityAvailable,
                null,
                dto.Notes);
            await _movementRepository.CreateAsync(movement);

            _logger.LogInformation("Stock removed: Product {ProductId}, Quantity {Quantity}", productId, dto.Quantity);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing stock for product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to remove stock" });
        }
        finally
        {
            if (lockAcquired)
            {
                await _distributedLock.ReleaseLockAsync(lockKey);
            }
        }
    }

    [HttpGet("product/{productId}/movements")]
    public async Task<ActionResult<List<StockMovement>>> GetStockMovements(Guid productId)
    {
        try
        {
            var movements = await _movementRepository.GetByProductIdAsync(productId);
            return Ok(movements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock movements for product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to retrieve stock movements" });
        }
    }

    private static InventoryItemDto MapToDto(InventoryItem item)
    {
        return new InventoryItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Sku = item.Sku,
            QuantityAvailable = item.QuantityAvailable,
            QuantityReserved = item.QuantityReserved,
            TotalQuantity = item.TotalQuantity,
            ReorderLevel = item.ReorderLevel,
            MaxStockLevel = item.MaxStockLevel,
            IsLowStock = item.IsLowStock,
            LastRestocked = item.LastRestocked,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
