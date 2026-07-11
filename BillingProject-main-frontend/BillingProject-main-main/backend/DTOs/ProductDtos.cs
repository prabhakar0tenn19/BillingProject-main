using System.ComponentModel.DataAnnotations;

namespace BillingSystem.DTOs;

// ─── Product Request DTOs ─────────────────────────────────────────────────────

/// <summary>
/// Used when CREATING a product. basePrice is accepted here (company sets internal price).
/// </summary>
public record CreateProductRequest(
    [Required(ErrorMessage = "Product name is required")]
    string Name,

    [Required(ErrorMessage = "Model number is required")]
    string ModelNumber,

    [Required(ErrorMessage = "Category is required")]
    string CategoryId,

    string? Description,

    /// <summary>
    /// INTERNAL ONLY — stored in DB, never returned via public API.
    /// This is the company's default/reference price.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Base price must be greater than 0")]
    decimal BasePrice,

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    int Stock,

    string? ImageUrl = null,
    string? ImagePublicId = null
);

/// <summary>Full update of a product. basePrice can be updated internally.</summary>
public record UpdateProductRequest(
    [Required] string Name,
    [Required] string ModelNumber,
    [Required] string CategoryId,
    string? Description,
    [Range(0.01, double.MaxValue)] decimal BasePrice,
    [Range(0, int.MaxValue)] int Stock,
    bool IsActive,
    string? ImageUrl = null,
    string? ImagePublicId = null
);

/// <summary>Partial stock update — can add to or set stock directly.</summary>
public record UpdateStockRequest(
    int Quantity,
    /// <summary>"add" = add to existing stock | "set" = replace stock value</summary>
    string Operation = "add"
);

// ─── Product Response DTOs ────────────────────────────────────────────────────

/// <summary>
/// ⚠️ INTENTIONALLY EXCLUDES basePrice.
/// This is returned by ALL public product endpoints.
/// The basePrice field does not exist in this record — it cannot leak accidentally.
/// </summary>
public record ProductResponse(
    string Id,
    string Name,
    string ModelNumber,
    string CategoryId,
    string CategoryName,
    string HsnCode,
    string? Description,
    int Stock,
    bool IsActive,
    string? ImageUrl,
    string? ImagePublicId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
