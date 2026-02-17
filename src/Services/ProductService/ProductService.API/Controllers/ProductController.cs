using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using ProductService.Application.Handlers;
using ProductService.Application.Queries;
using ProductService.Infrastructure.Cache;
using EventBus.Abstractions;
using ProductService.Infrastructure.Events;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly CreateProductHandler _createProductHandler;
    private readonly UpdateProductHandler _updateProductHandler;
    private readonly GetProductsHandler _getProductsHandler;
    private readonly GetProductByIdHandler _getProductByIdHandler;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ProductController> _logger;

    public ProductController(
        CreateProductHandler createProductHandler,
        UpdateProductHandler updateProductHandler,
        GetProductsHandler getProductsHandler,
        GetProductByIdHandler getProductByIdHandler,
        ICacheService cacheService,
        IEventBus eventBus,
        ILogger<ProductController> logger)
    {
        _createProductHandler = createProductHandler;
        _updateProductHandler = updateProductHandler;
        _getProductsHandler = getProductsHandler;
        _getProductByIdHandler = getProductByIdHandler;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts([FromQuery] GetProductsQuery query)
    {
        try
        {
            var cacheKey = GenerateCacheKey(query);
            var cachedResult = await _cacheService.GetAsync<PagedResult<ProductDto>>(cacheKey);

            if (cachedResult != null)
            {
                _logger.LogInformation("Returning cached products for key: {CacheKey}", cacheKey);
                return Ok(cachedResult);
            }

            var result = await _getProductsHandler.HandleAsync(query);

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while retrieving products"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        try
        {
            var cacheKey = $"product:{id}";
            var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey);

            if (cachedProduct != null)
            {
                _logger.LogInformation("Returning cached product: {ProductId}", id);
                
                await _eventBus.PublishAsync(new ProductViewedEvent
                {
                    ProductId = id,
                    ViewedAt = DateTime.UtcNow
                });

                return Ok(cachedProduct);
            }

            var query = new GetProductByIdQuery(id);
            var product = await _getProductByIdHandler.HandleAsync(query);

            if (product == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Product with ID '{id}' not found"));
            }

            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(10));

            await _eventBus.PublishAsync(new ProductViewedEvent
            {
                ProductId = id,
                ViewedAt = DateTime.UtcNow
            });

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by ID: {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while retrieving the product"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductCommand command)
    {
        try
        {
            var product = await _createProductHandler.HandleAsync(command);

            await _cacheService.RemoveByPrefixAsync("products:");

            await _eventBus.PublishAsync(new ProductCreatedEvent
            {
                ProductId = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Price = product.Price,
                CategoryId = product.CategoryId,
                SellerId = product.SellerId
            });

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Common.Exceptions.NotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while creating the product"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        try
        {
            command.Id = id;
            var product = await _updateProductHandler.HandleAsync(command);

            await _cacheService.RemoveAsync($"product:{id}");
            await _cacheService.RemoveByPrefixAsync("products:");

            await _eventBus.PublishAsync(new ProductUpdatedEvent
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                StockQuantity = product.StockQuantity
            });

            return Ok(product);
        }
        catch (Common.Exceptions.NotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while updating the product"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        try
        {
            var query = new GetProductByIdQuery(id);
            var product = await _getProductByIdHandler.HandleAsync(query);

            if (product == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Product with ID '{id}' not found"));
            }

            var updateCommand = new UpdateProductCommand
            {
                Id = id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = 0,
                CategoryId = product.CategoryId
            };

            await _updateProductHandler.HandleAsync(updateCommand);

            await _cacheService.RemoveAsync($"product:{id}");
            await _cacheService.RemoveByPrefixAsync("products:");

            await _eventBus.PublishAsync(new ProductDeletedEvent
            {
                ProductId = id,
                SKU = product.SKU
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting the product"));
        }
    }

    private static string GenerateCacheKey(GetProductsQuery query)
    {
        return $"products:{query.PageNumber}:{query.PageSize}:{query.SearchTerm}:{query.CategoryId}:{query.SellerId}:{query.MinPrice}:{query.MaxPrice}:{query.Status}:{query.InStock}:{query.SortBy}:{query.Ascending}";
    }
}
