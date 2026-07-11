using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BillingSystem.Models;

/// <summary>
/// Company-wide configuration stored as a SINGLE document in MongoDB.
/// This is the single source of truth for company info, GST settings,
/// and the atomic invoice number counter.
///
/// Invoice number generation uses MongoDB's findOneAndUpdate with $inc
/// which is atomic — safe for concurrent requests (no duplicates, no gaps).
/// </summary>
public class Settings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // ── Company Identity ──────────────────────────────────────────────────
    [BsonElement("companyName")]
    public string CompanyName { get; set; } = "My Company";

    [BsonElement("companyAddress")]
    public string CompanyAddress { get; set; } = string.Empty;

    [BsonElement("companyPhone")]
    public string CompanyPhone { get; set; } = string.Empty;

    [BsonElement("companyEmail")]
    public string CompanyEmail { get; set; } = string.Empty;

    [BsonElement("gstin")]
    public string Gstin { get; set; } = string.Empty;

    // ── Bank Details (shown on invoice footer) ────────────────────────────
    [BsonElement("bankName")]
    public string BankName { get; set; } = string.Empty;

    [BsonElement("bankAccount")]
    public string BankAccount { get; set; } = string.Empty;

    [BsonElement("bankIfsc")]
    public string BankIfsc { get; set; } = string.Empty;

    // ── Digital Signature (stored on Cloudinary, embedded in PDF) ─────────
    [BsonElement("signatureCloudinaryUrl")]
    public string? SignatureCloudinaryUrl { get; set; }

    /// <summary>Cloudinary public_id used for deletion/replacement</summary>
    [BsonElement("signaturePublicId")]
    public string? SignaturePublicId { get; set; }

    // ── Invoice Settings ──────────────────────────────────────────────────
    /// <summary>
    /// Prefix for invoice numbers. Default: "INV"
    /// Generates: INV-2024-0001
    /// </summary>
    [BsonElement("invoicePrefix")]
    public string InvoicePrefix { get; set; } = "INV";

    /// <summary>
    /// ATOMIC COUNTER — incremented using MongoDB $inc operator.
    /// Never update this field directly from application code.
    /// Use InvoiceService.GenerateInvoiceNumberAsync() only.
    /// </summary>
    [BsonElement("nextInvoiceNumber")]
    public int NextInvoiceNumber { get; set; } = 1;

    /// <summary>GST rate percentage. Default 5% for sanitaryware.</summary>
    [BsonElement("gstRate")]
    public decimal GstRate { get; set; } = 5.0m;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
