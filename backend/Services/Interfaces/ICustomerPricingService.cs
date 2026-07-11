using BillingSystem.DTOs;

namespace BillingSystem.Services.Interfaces;

public interface ICustomerPricingService
{
    /// <summary>Get all price overrides for a specific customer (party).</summary>
    Task<List<PricingResponse>> GetPricingForCustomerAsync(string customerId);

    /// <summary>
    /// Set a custom price for a specific product for this customer.
    /// Uses MongoDB upsert — creates or updates, never duplicates.
    /// </summary>
    Task<PricingResponse> SetPricingAsync(string customerId, SetPricingRequest request);

    /// <summary>Update just the price for an existing override.</summary>
    Task<PricingResponse?> UpdatePricingAsync(string customerId, string productId, UpdatePricingRequest request);

    /// <summary>Remove price override. Product will fall back to basePrice when billing.</summary>
    Task<bool> DeletePricingAsync(string customerId, string productId);

    /// <summary>
    /// KEY BILLING METHOD — Returns ALL active products with their EFFECTIVE price
    /// for this customer. Used by the New Bill page.
    ///
    /// EffectivePrice resolution:
    ///   1. If override exists in customer_pricing → use negotiatedPrice
    ///   2. If no override → use product.basePrice (but HasCustomPrice = false)
    ///
    /// The UI should never label the price as "base" or "default" — just show the number.
    /// </summary>
    Task<List<BillReadyProductResponse>> GetBillReadyProductsAsync(string customerId);
}
