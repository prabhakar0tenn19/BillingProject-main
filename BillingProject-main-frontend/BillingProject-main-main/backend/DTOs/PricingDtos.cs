using System.ComponentModel.DataAnnotations;

namespace BillingSystem.DTOs;

// ─── Pricing Request DTOs ─────────────────────────────────────────────────────

public record SetPricingRequest(
    [Required(ErrorMessage = "Product ID is required")]
    string ProductId,

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    decimal NegotiatedPrice
);

public record UpdatePricingRequest(
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    decimal NegotiatedPrice
);

// ─── Pricing Response DTOs ────────────────────────────────────────────────────

/// <summary>Full pricing record for a customer-product pair.</summary>
public record PricingResponse(
    string Id,
    string CustomerId,
    string CustomerName,
    string ProductId,
    string ProductName,
    string ModelNumber,
    string CategoryName,
    string HsnCode,
    decimal NegotiatedPrice,
    DateTime UpdatedAt
);

/// <summary>
/// Used by the New Bill page — returns ALL products with their effective price
/// for the selected party. The UI uses this to auto-populate price when
/// a product is added to the bill.
///
/// EffectivePrice = negotiatedPrice if override exists, else product.basePrice
/// HasCustomPrice = true if a party-specific override was found
///
/// The UI should NOT label "base price" anywhere — just show the effective price.
/// </summary>
public record BillReadyProductResponse(
    string ProductId,
    string ProductName,
    string ModelNumber,
    string CategoryId,
    string CategoryName,
    string HsnCode,
    int Stock,
    decimal EffectivePrice,
    bool HasCustomPrice
);
