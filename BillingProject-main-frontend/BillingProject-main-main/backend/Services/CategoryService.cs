using BillingSystem.DTOs;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using BillingSystem.Services.Interfaces;
using MongoDB.Driver;

namespace BillingSystem.Services;

/// <summary>
/// CRUD operations for product categories.
/// Soft delete — isActive = false, not hard delete.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly MongoDbContext _db;

    public CategoryService(MongoDbContext db) => _db = db;

    public async Task<List<CategoryResponse>> GetAllAsync()
    {
        var categories = await _db.Categories
            .Find(c => c.IsActive)
            .SortBy(c => c.Name)
            .ToListAsync();

        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryResponse?> GetByIdAsync(string id)
    {
        var cat = await _db.Categories.Find(c => c.Id == id).FirstOrDefaultAsync();
        return cat is null ? null : ToDto(cat);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name        = request.Name.Trim(),
            Description = request.Description?.Trim(),
            HsnCode     = request.HsnCode.Trim(),
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

        await _db.Categories.InsertOneAsync(category);
        return ToDto(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(string id, UpdateCategoryRequest request)
    {
        var update = Builders<Category>.Update
            .Set(c => c.Name,        request.Name.Trim())
            .Set(c => c.Description, request.Description?.Trim())
            .Set(c => c.HsnCode,     request.HsnCode.Trim())
            .Set(c => c.UpdatedAt,   DateTime.UtcNow);

        var result = await _db.Categories.FindOneAndUpdateAsync<Category, Category>(
            c => c.Id == id,
            update,
            new FindOneAndUpdateOptions<Category, Category> { ReturnDocument = ReturnDocument.After }
        );

        return result is null ? null : ToDto(result);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        // Soft delete — preserves products linked to this category
        var result = await _db.Categories.UpdateOneAsync(
            c => c.Id == id,
            Builders<Category>.Update
                .Set(c => c.IsActive,  false)
                .Set(c => c.UpdatedAt, DateTime.UtcNow)
        );

        return result.ModifiedCount > 0;
    }

    // ─── DTO Mapping ──────────────────────────────────────────────────────────

    private static CategoryResponse ToDto(Category c) => new(
        Id:          c.Id!,
        Name:        c.Name,
        Description: c.Description,
        HsnCode:     c.HsnCode,
        IsActive:    c.IsActive,
        CreatedAt:   c.CreatedAt,
        UpdatedAt:   c.UpdatedAt
    );
}
