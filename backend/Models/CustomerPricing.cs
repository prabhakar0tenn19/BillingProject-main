using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BillingSystem.Models;

/// <summary>
/// Per-party, per-product custom price override.
///
/// This collection allows different pricing for the same product
/// for different customers (parties). Key business feature.
///
/// Compound unique index enforced on (customerId, productId):
///   → Only ONE price override allowed per product per party
///   → To change price, UPDATE the existing record (not insert new)
///
/// Price Resolution Logic (used in billing):
///   1. Query this collection for (customerId, productId)
///   2. Found → use negotiatedPrice
///   3. Not found → fall back to product.basePrice (internal, never labeled)
///
/// CustomerName and ProductName are denormalized for display
/// without expensive lookups.
/// </summary>
public class CustomerPricing
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("customerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>Denormalized for display — avoids customer lookup on list.</summary>
    [BsonElement("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [BsonElement("productId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>Denormalized for display — avoids product lookup on list.</summary>
    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("modelNumber")]
    public string ModelNumber { get; set; } = string.Empty;

    /// <summary>
    /// The negotiated/agreed price for this specific party.
    /// This overrides product.basePrice when billing this customer.
    /// </summary>
    [BsonElement("negotiatedPrice")]
    public decimal NegotiatedPrice { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
