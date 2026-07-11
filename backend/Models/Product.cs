using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BillingSystem.Models;

/// <summary>
/// Product in the master catalog.
///
/// ⚠️ IMPORTANT — basePrice field:
///   basePrice is an INTERNAL reference price only.
///   It is NEVER returned to the frontend via any public API.
///   ProductResponseDto intentionally excludes this field.
///
///   When billing:
///     1. Check customer_pricing collection for a party-specific override
///     2. If found → use negotiatedPrice
///     3. If NOT found → use basePrice as fallback (internal only, never labeled "base price" on invoice)
///
/// CategoryName and HsnCode are denormalized here for read speed
/// (avoids join when building invoices).
/// </summary>
public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Model/SKU number (e.g. SH-001). Shown on invoice.</summary>
    [BsonElement("modelNumber")]
    public string ModelNumber { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>Denormalized from Category for fast invoice building.</summary>
    [BsonElement("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Denormalized from Category. Printed on GST invoice.</summary>
    [BsonElement("hsnCode")]
    public string HsnCode { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    /// <summary>
    /// ⚠️ INTERNAL ONLY — never exposed in public API responses.
    /// This is the manufacturer's base/default price.
    /// Party-specific prices in customer_pricing override this.
    /// </summary>
    [BsonElement("basePrice")]
    public decimal BasePrice { get; set; }

    [BsonElement("stock")]
    public int Stock { get; set; }

    [BsonElement("imageUrl")]
    public string? ImageUrl { get; set; }

    /// <summary>Cloudinary public_id used for deletion/replacement</summary>
    [BsonElement("imagePublicId")]
    public string? ImagePublicId { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
