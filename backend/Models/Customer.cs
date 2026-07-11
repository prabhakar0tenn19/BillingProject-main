using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BillingSystem.Models;

/// <summary>
/// A customer/party in the billing system.
///
/// Called "party" in business language — these are the businesses
/// or individuals that the company sells products to.
///
/// Each customer can have custom product prices defined in the
/// customer_pricing collection.
/// </summary>
public class Customer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Business/party name (e.g. "Sharma Hardware Pvt Ltd")</summary>
    [BsonElement("partyName")]
    public string PartyName { get; set; } = string.Empty;

    [BsonElement("contactPerson")]
    public string? ContactPerson { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("billingAddress")]
    public string? BillingAddress { get; set; }

    [BsonElement("shippingAddress")]
    public string? ShippingAddress { get; set; }

    /// <summary>GST Identification Number — required for B2B GST invoices.</summary>
    [BsonElement("gstin")]
    public string? Gstin { get; set; }

    [BsonElement("panNumber")]
    public string? PanNumber { get; set; }

    /// <summary>Soft delete flag. Deleted customers are never hard-removed (audit trail).</summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
