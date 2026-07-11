using BillingSystem.DTOs;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using BillingSystem.Services.Interfaces;
using MongoDB.Driver;

namespace BillingSystem.Services;

/// <summary>
/// Manages per-party, per-product price overrides.
///
/// KEY METHOD: GetBillReadyProductsAsync()
///   Used by the New Bill wizard to load all products with their effective price
///   for the selected party. This is the core of the custom pricing feature.
///
/// Price resolution:
///   1. Load all active products (with basePrice via internal API)
///   2. Load all pricing overrides for this customer
///   3. For each product: override exists → use it; else → use basePrice
///   4. Return BillReadyProductResponse (never labels price as "base")
/// </summary>
public class CustomerPricingService : ICustomerPricingService
{
    private readonly MongoDbContext _db;
    private readonly IProductService _productService;

    public CustomerPricingService(MongoDbContext db, IProductService productService)
    {
        _db             = db;
        _productService = productService;
    }

    public async Task<List<PricingResponse>> GetPricingForCustomerAsync(string customerId)
    {
        var pricingList = await _db.CustomerPricing
            .Find(p => p.CustomerId == customerId && p.IsActive)
            .SortBy(p => p.ProductName)
            .ToListAsync();

        return pricingList.Select(ToDto).ToList();
    }

    public async Task<PricingResponse> SetPricingAsync(string customerId, SetPricingRequest request)
    {
        // Validate product exists
        var product = await _productService.GetInternalAsync(request.ProductId)
            ?? throw new KeyNotFoundException($"Product '{request.ProductId}' not found or inactive");

        // Validate customer exists
        var customer = await _db.Customers.Find(c => c.Id == customerId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Customer '{customerId}' not found");

        // UPSERT — update if exists, insert if new
        // The unique index on (customerId, productId) enforces only one record per pair
        var filter = Builders<CustomerPricing>.Filter.And(
            Builders<CustomerPricing>.Filter.Eq(p => p.CustomerId, customerId),
            Builders<CustomerPricing>.Filter.Eq(p => p.ProductId,  request.ProductId)
        );

        var update = Builders<CustomerPricing>.Update
            .Set(p => p.CustomerName,     customer.PartyName)
            .Set(p => p.ProductName,      product.Name)
            .Set(p => p.ModelNumber,      product.ModelNumber)
            .Set(p => p.NegotiatedPrice,  request.NegotiatedPrice)
            .Set(p => p.IsActive,         true)
            .Set(p => p.UpdatedAt,        DateTime.UtcNow)
            .SetOnInsert(p => p.CustomerId, customerId)  // ensures customerId on insert
            .SetOnInsert(p => p.ProductId,  request.ProductId);

        var result = await _db.CustomerPricing.FindOneAndUpdateAsync<CustomerPricing, CustomerPricing>(
            p => p.CustomerId == customerId && p.ProductId == request.ProductId,
            update,
            new FindOneAndUpdateOptions<CustomerPricing, CustomerPricing>
            {
                IsUpsert       = true,
                ReturnDocument = ReturnDocument.After
            }
        );

        return ToDto(result);
    }

    public async Task<PricingResponse?> UpdatePricingAsync(string customerId, string productId, UpdatePricingRequest request)
    {
        var result = await _db.CustomerPricing.FindOneAndUpdateAsync<CustomerPricing, CustomerPricing>(
            p => p.CustomerId == customerId && p.ProductId == productId,
            Builders<CustomerPricing>.Update
                .Set(p => p.NegotiatedPrice, request.NegotiatedPrice)
                .Set(p => p.UpdatedAt,       DateTime.UtcNow),
            new FindOneAndUpdateOptions<CustomerPricing, CustomerPricing> { ReturnDocument = ReturnDocument.After }
        );

        return result is null ? null : ToDto(result);
    }

    public async Task<bool> DeletePricingAsync(string customerId, string productId)
    {
        // Soft delete — set isActive = false
        // Product falls back to basePrice when billing (invisible to user)
        var result = await _db.CustomerPricing.UpdateOneAsync(
            p => p.CustomerId == customerId && p.ProductId == productId,
            Builders<CustomerPricing>.Update
                .Set(p => p.IsActive,  false)
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
        );

        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Returns ALL active products with their effective price for the given customer.
    /// This is what the New Bill wizard calls when a party is selected.
    ///
    /// Algorithm:
    ///   1. Load all active products (internal — includes basePrice)
    ///   2. Load all active pricing overrides for this customer
    ///   3. Build a lookup dictionary: productId → negotiatedPrice
    ///   4. For each product:
    ///      - If override exists → effectivePrice = negotiatedPrice, hasCustomPrice = true
    ///      - Else              → effectivePrice = basePrice,        hasCustomPrice = false
    ///
    /// The word "base" or "default" never appears in the response.
    /// </summary>
    public async Task<List<BillReadyProductResponse>> GetBillReadyProductsAsync(string customerId)
    {
        // Load all products (internal method has basePrice)
        var allProducts = await _productService.GetAllInternalAsync();

        // Load all overrides for this customer into a fast lookup dict
        var overrides = await _db.CustomerPricing
            .Find(p => p.CustomerId == customerId && p.IsActive)
            .ToListAsync();

        var overrideLookup = overrides.ToDictionary(o => o.ProductId, o => o.NegotiatedPrice);

        return allProducts.Select(p =>
        {
            bool hasCustomPrice = overrideLookup.TryGetValue(p.Id!, out var customPrice);
            decimal effectivePrice = hasCustomPrice ? customPrice : p.BasePrice;

            return new BillReadyProductResponse(
                ProductId:      p.Id!,
                ProductName:    p.Name,
                ModelNumber:    p.ModelNumber,
                CategoryId:     p.CategoryId,
                CategoryName:   p.CategoryName,
                HsnCode:        p.HsnCode,
                Stock:          p.Stock,
                EffectivePrice: effectivePrice,
                HasCustomPrice: hasCustomPrice
            );
        })
        .OrderBy(p => p.CategoryName)
        .ThenBy(p => p.ProductName)
        .ToList();
    }

    // ─── DTO Mapping ──────────────────────────────────────────────────────────

    private static PricingResponse ToDto(CustomerPricing p)
    {
        // We need to get category and HSN from the product — but since these are
        // denormalized at pricing creation time, we need to fetch or store them.
        // For now, returning without categoryName/hsnCode (not in CustomerPricing model)
        // The controller that needs these should use GetBillReadyProductsAsync instead.
        return new PricingResponse(
            Id:              p.Id!,
            CustomerId:      p.CustomerId,
            CustomerName:    p.CustomerName,
            ProductId:       p.ProductId,
            ProductName:     p.ProductName,
            ModelNumber:     p.ModelNumber,
            CategoryName:    string.Empty,   // populated by GetBillReadyProductsAsync
            HsnCode:         string.Empty,   // populated by GetBillReadyProductsAsync
            NegotiatedPrice: p.NegotiatedPrice,
            UpdatedAt:       p.UpdatedAt
        );
    }
}
