using BillingSystem.DTOs;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using BillingSystem.Services.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BillingSystem.Services;

/// <summary>
/// Product CRUD with strict basePrice isolation and Cloudinary product image uploads.
///
/// Architecture rule: basePrice NEVER appears in public-facing ProductResponse.
/// Two separate methods handle this:
///   - GetByIdAsync / GetAllAsync   → returns ProductResponse (no basePrice)
///   - GetInternalAsync             → returns Product model (has basePrice)
///
/// Only InvoiceService and CustomerPricingService may call internal methods.
/// </summary>
public class ProductService : IProductService
{
    private readonly MongoDbContext _db;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<ProductService> _logger;

    public ProductService(MongoDbContext db, IConfiguration config, ILogger<ProductService> logger)
    {
        _db     = db;
        _logger = logger;

        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    // ─── Public API (no basePrice) ────────────────────────────────────────────

    public async Task<List<ProductResponse>> GetAllAsync(string? categoryId = null, string? search = null)
    {
        var filterBuilder = Builders<Product>.Filter;
        var filter = filterBuilder.Eq(p => p.IsActive, true);

        if (!string.IsNullOrEmpty(categoryId))
            filter &= filterBuilder.Eq(p => p.CategoryId, categoryId);

        if (!string.IsNullOrEmpty(search))
        {
            var searchRegex = new MongoDB.Bson.BsonRegularExpression(search, "i");
            filter &= filterBuilder.Or(
                filterBuilder.Regex(p => p.Name, searchRegex),
                filterBuilder.Regex(p => p.ModelNumber, searchRegex)
            );
        }

        var products = await _db.Products
            .Find(filter)
            .SortBy(p => p.CategoryName)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return products.Select(ToPublicDto).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(string id)
    {
        var product = await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        return product is null ? null : ToPublicDto(product);
    }

    // ─── Create / Update / Delete ─────────────────────────────────────────────

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        // Fetch category to get HSN code (denormalized into product)
        var category = await _db.Categories
            .Find(c => c.Id == request.CategoryId && c.IsActive)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Category '{request.CategoryId}' not found or inactive");

        var product = new Product
        {
            Name         = request.Name.Trim(),
            ModelNumber  = request.ModelNumber.Trim().ToUpperInvariant(),
            CategoryId   = request.CategoryId,
            CategoryName = category.Name,
            HsnCode      = category.HsnCode,
            Description  = request.Description?.Trim(),
            BasePrice    = request.BasePrice,    // stored internally, never returned publicly
            Stock        = request.Stock,
            ImageUrl     = request.ImageUrl,
            ImagePublicId = request.ImagePublicId,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        await _db.Products.InsertOneAsync(product);
        return ToPublicDto(product);
    }

    public async Task<ProductResponse?> UpdateAsync(string id, UpdateProductRequest request)
    {
        // Fetch category to refresh denormalized fields
        var category = await _db.Categories
            .Find(c => c.Id == request.CategoryId && c.IsActive)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Category '{request.CategoryId}' not found or inactive");

        var update = Builders<Product>.Update
            .Set(p => p.Name,         request.Name.Trim())
            .Set(p => p.ModelNumber,  request.ModelNumber.Trim().ToUpperInvariant())
            .Set(p => p.CategoryId,   request.CategoryId)
            .Set(p => p.CategoryName, category.Name)
            .Set(p => p.HsnCode,      category.HsnCode)
            .Set(p => p.Description,  request.Description?.Trim())
            .Set(p => p.BasePrice,    request.BasePrice)  // internal update
            .Set(p => p.Stock,        request.Stock)
            .Set(p => p.ImageUrl,     request.ImageUrl)
            .Set(p => p.ImagePublicId, request.ImagePublicId)
            .Set(p => p.IsActive,     request.IsActive)
            .Set(p => p.UpdatedAt,    DateTime.UtcNow);

        var result = await _db.Products.FindOneAndUpdateAsync<Product, Product>(
            p => p.Id == id,
            update,
            new FindOneAndUpdateOptions<Product, Product> { ReturnDocument = ReturnDocument.After }
        );

        return result is null ? null : ToPublicDto(result);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _db.Products.UpdateOneAsync(
            p => p.Id == id,
            Builders<Product>.Update
                .Set(p => p.IsActive,  false)
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
        );

        return result.ModifiedCount > 0;
    }

    public async Task<ProductResponse?> UpdateStockAsync(string id, UpdateStockRequest request)
    {
        UpdateDefinition<Product> update;

        if (request.Operation == "set")
        {
            // Replace stock value directly
            update = Builders<Product>.Update
                .Set(p => p.Stock,     request.Quantity)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);
        }
        else
        {
            // Default: "add" — increment/decrement current stock
            update = Builders<Product>.Update
                .Inc(p => p.Stock, request.Quantity)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);
        }

        var result = await _db.Products.FindOneAndUpdateAsync<Product, Product>(
            p => p.Id == id,
            update,
            new FindOneAndUpdateOptions<Product, Product> { ReturnDocument = ReturnDocument.After }
        );

        return result is null ? null : ToPublicDto(result);
    }

    // ─── Image Upload / Deletion ──────────────────────────────────────────────

    public async Task<ProductResponse?> UploadImageAsync(string id, IFormFile file)
    {
        var product = await _db.Products.Find(p => p.Id == id && p.IsActive).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Product '{id}' not found");

        // Delete old image from Cloudinary if it exists
        if (!string.IsNullOrEmpty(product.ImagePublicId))
        {
            await _cloudinary.DestroyAsync(new DeletionParams(product.ImagePublicId));
            _logger.LogInformation("Deleted old product image: {PublicId}", product.ImagePublicId);
        }

        // Upload new image
        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File           = new FileDescription(file.FileName, stream),
            Folder         = "product_images",
            Transformation = new Transformation().Width(500).Height(500).Crop("fit"),
            Overwrite      = true
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");

        _logger.LogInformation("Product image uploaded: {Url}", uploadResult.SecureUrl);

        var update = Builders<Product>.Update
            .Set(p => p.ImageUrl,      uploadResult.SecureUrl.ToString())
            .Set(p => p.ImagePublicId, uploadResult.PublicId)
            .Set(p => p.UpdatedAt,     DateTime.UtcNow);

        var updated = await _db.Products.FindOneAndUpdateAsync<Product, Product>(
            p => p.Id == id,
            update,
            new FindOneAndUpdateOptions<Product, Product> { ReturnDocument = ReturnDocument.After }
        );

        return updated is null ? null : ToPublicDto(updated);
    }

    public async Task<ProductResponse?> DeleteImageAsync(string id)
    {
        var product = await _db.Products.Find(p => p.Id == id && p.IsActive).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Product '{id}' not found");

        if (!string.IsNullOrEmpty(product.ImagePublicId))
        {
            await _cloudinary.DestroyAsync(new DeletionParams(product.ImagePublicId));
            _logger.LogInformation("Deleted product image: {PublicId}", product.ImagePublicId);
        }

        var update = Builders<Product>.Update
            .Set(p => p.ImageUrl,      (string?)null)
            .Set(p => p.ImagePublicId, (string?)null)
            .Set(p => p.UpdatedAt,     DateTime.UtcNow);

        var updated = await _db.Products.FindOneAndUpdateAsync<Product, Product>(
            p => p.Id == id,
            update,
            new FindOneAndUpdateOptions<Product, Product> { ReturnDocument = ReturnDocument.After }
        );

        return updated is null ? null : ToPublicDto(updated);
    }

    // ─── Internal API (includes basePrice — for services only) ───────────────

    /// <summary>Returns full Product model WITH basePrice. Never call from a controller.</summary>
    public async Task<Product?> GetInternalAsync(string id)
        => await _db.Products.Find(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();

    /// <summary>Returns all active products with basePrice for billing resolution.</summary>
    public async Task<List<Product>> GetAllInternalAsync()
        => await _db.Products.Find(p => p.IsActive).ToListAsync();

    // ─── DTO Mapping ──────────────────────────────────────────────────────────

    /// <summary>
    /// Maps Product model → ProductResponse DTO.
    /// basePrice field is deliberately ABSENT from ProductResponse — cannot leak.
    /// </summary>
    private static ProductResponse ToPublicDto(Product p) => new(
        Id:            p.Id!,
        Name:          p.Name,
        ModelNumber:   p.ModelNumber,
        CategoryId:    p.CategoryId,
        CategoryName:  p.CategoryName,
        HsnCode:       p.HsnCode,
        Description:   p.Description,
        Stock:         p.Stock,
        IsActive:      p.IsActive,
        ImageUrl:      p.ImageUrl,
        ImagePublicId: p.ImagePublicId,
        CreatedAt:     p.CreatedAt,
        UpdatedAt:     p.UpdatedAt
    );
}
