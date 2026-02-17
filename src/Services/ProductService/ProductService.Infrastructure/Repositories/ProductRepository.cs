using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Domain.Specifications;
using ProductService.Infrastructure.Data;

namespace ProductService.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.SKU == sku && !p.IsDeleted, cancellationToken);
    }

    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        ProductSpecification specification,
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        query = specification.Apply(query);

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, sortBy, ascending);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<Product>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.SellerId == sellerId && !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);
        if (product != null)
        {
            product.SoftDelete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.SKU == sku && !p.IsDeleted, cancellationToken);
    }

    public async Task<List<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var searchLower = searchTerm.ToLowerInvariant();
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted &&
                (p.Name.ToLower().Contains(searchLower) ||
                 p.Description.ToLower().Contains(searchLower) ||
                 p.SKU.ToLower().Contains(searchLower)))
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Product> ApplySorting(
        IQueryable<Product> query,
        string? sortBy,
        bool ascending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            sortBy = "CreatedAt";

        query = sortBy.ToLowerInvariant() switch
        {
            "name" => ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
            "price" => ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
            "createdat" => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
            "viewcount" => ascending ? query.OrderBy(p => p.ViewCount) : query.OrderByDescending(p => p.ViewCount),
            "stock" => ascending ? query.OrderBy(p => p.StockQuantity) : query.OrderByDescending(p => p.StockQuantity),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        return query;
    }
}
