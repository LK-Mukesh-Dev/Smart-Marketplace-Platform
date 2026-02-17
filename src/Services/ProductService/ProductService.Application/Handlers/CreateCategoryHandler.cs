using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Handlers;

public class CreateCategoryHandler
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        if (await _categoryRepository.ExistsAsync(command.Name, cancellationToken))
        {
            throw new InvalidOperationException($"Category with name '{command.Name}' already exists");
        }

        if (command.ParentCategoryId.HasValue)
        {
            var parentCategory = await _categoryRepository.GetByIdAsync(command.ParentCategoryId.Value, cancellationToken);
            if (parentCategory == null)
            {
                throw new Common.Exceptions.NotFoundException($"Parent category with ID '{command.ParentCategoryId}' not found");
            }
        }

        var category = new Category(
            command.Name,
            command.Description,
            command.ParentCategoryId,
            command.DisplayOrder);

        var createdCategory = await _categoryRepository.CreateAsync(category, cancellationToken);

        return CategoryDto.FromEntity(createdCategory);
    }
}
