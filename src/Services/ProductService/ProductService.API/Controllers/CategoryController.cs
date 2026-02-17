using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using ProductService.Application.Handlers;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Cache;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly CreateCategoryHandler _createCategoryHandler;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(
        CreateCategoryHandler createCategoryHandler,
        ICategoryRepository categoryRepository,
        ICacheService cacheService,
        ILogger<CategoryController> logger)
    {
        _createCategoryHandler = createCategoryHandler;
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAllCategories()
    {
        try
        {
            var cacheKey = "categories:all";
            var cachedCategories = await _cacheService.GetAsync<List<CategoryDto>>(cacheKey);

            if (cachedCategories != null)
            {
                return Ok(cachedCategories);
            }

            var categories = await _categoryRepository.GetAllAsync();
            var categoryDtos = categories.Select(c => CategoryDto.FromEntity(c, true)).ToList();

            await _cacheService.SetAsync(cacheKey, categoryDtos, TimeSpan.FromHours(1));

            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while retrieving categories"));
        }
    }

    [HttpGet("root")]
    public async Task<ActionResult<List<CategoryDto>>> GetRootCategories()
    {
        try
        {
            var cacheKey = "categories:root";
            var cachedCategories = await _cacheService.GetAsync<List<CategoryDto>>(cacheKey);

            if (cachedCategories != null)
            {
                return Ok(cachedCategories);
            }

            var categories = await _categoryRepository.GetRootCategoriesAsync();
            var categoryDtos = categories.Select(c => CategoryDto.FromEntity(c, true)).ToList();

            await _cacheService.SetAsync(cacheKey, categoryDtos, TimeSpan.FromHours(1));

            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root categories");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while retrieving root categories"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(Guid id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Category with ID '{id}' not found"));
            }

            return Ok(CategoryDto.FromEntity(category, true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category: {CategoryId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while retrieving the category"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        try
        {
            var category = await _createCategoryHandler.HandleAsync(command);

            await _cacheService.RemoveByPrefixAsync("categories:");

            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
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
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while creating the category"));
        }
    }
}
