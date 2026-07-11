using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BillingSystem.Models;

/// <summary>
/// Product category (e.g. Taps, Showers, Drain Covers, Washbasins).
///
/// Seeded on first run with 4 defaults. Can be expanded — no limit.
/// Each category carries an HSN code used on GST invoices.
///
/// HSN reference for sanitaryware:
///   Taps      → 8481
///   Showers   → 8481
///   Drain Covers → 7325
///   Washbasins → 6910
/// </summary>
public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    /// <summary>HSN code for GST compliance. Inherited by products in this category.</summary>
    [BsonElement("hsnCode")]
    public string HsnCode { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
