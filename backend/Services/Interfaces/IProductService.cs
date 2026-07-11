using BillingSystem.DTOs;
using BillingSystem.Models;
using Microsoft.AspNetCore.Http;

namespace BillingSystem.Services.Interfaces;

public interface IProductService
{
    /// <summary>Get all active products. basePrice is NEVER included in response.</summary>
    Task<List<ProductResponse>> GetAllAsync(string? categoryId = null, string? search = null);

    /// <summary>Get product by ID. basePrice is NEVER included in response.</summary>
    Task<ProductResponse?> GetByIdAsync(string id);

    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse?> UpdateAsync(string id, UpdateProductRequest request);

    /// <summary>Soft delete.</summary>
    Task<bool> DeleteAsync(string id);

    Task<ProductResponse?> UpdateStockAsync(string id, UpdateStockRequest request);

    /// <summary>Uploads product image to Cloudinary and links to product.</summary>
    Task<ProductResponse?> UploadImageAsync(string id, IFormFile file);

    /// <summary>Removes product image from Cloudinary and product link.</summary>
    Task<ProductResponse?> DeleteImageAsync(string id);

    /// <summary>
    /// INTERNAL — returns the full Product model INCLUDING basePrice.
    /// Only called by CustomerPricingService and InvoiceService.
    /// NEVER call this from a controller.
    /// </summary>
    Task<Product?> GetInternalAsync(string id);

    /// <summary>INTERNAL — returns all active products with basePrice for billing resolution.</summary>
    Task<List<Product>> GetAllInternalAsync();
}
