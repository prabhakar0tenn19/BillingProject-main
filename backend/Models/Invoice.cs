using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BillingSystem.Models;

/// <summary>
/// Invoice document with embedded items and immutable snapshots.
///
/// Design decisions:
///
/// 1. EMBEDDED ITEMS — invoice items are stored inside the invoice document
///    (not a separate collection). This means one DB read = full invoice.
///    Faster retrieval, no joins, simpler queries.
///
/// 2. SNAPSHOT PATTERN — CustomerSnapshot and CompanySnapshot store the
///    exact details at the time of billing. Even if the company later
///    changes its address or a customer changes their GSTIN, old invoices
///    remain historically accurate. This is how real accounting software works.
///
/// 3. ATOMIC INVOICE NUMBERS — generated via MongoDB $inc on settings.nextInvoiceNumber
///    Format: INV-2024-0001
///
/// GST split for PDF: though stored as totalGst, split as CGST 2.5% + SGST 2.5% on PDF.
/// </summary>
public class Invoice
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Auto-generated: INV-YYYY-NNNN format. Unique.</summary>
    [BsonElement("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [BsonElement("invoiceDate")]
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    [BsonElement("dueDate")]
    public DateTime? DueDate { get; set; }

    /// <summary>Use InvoiceStatus constants: "pending" | "paid" | "cancelled"</summary>
    [BsonElement("status")]
    public string Status { get; set; } = InvoiceStatus.Pending;

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    /// <summary>Use PaymentMode constants: "cash" | "cheque" | "neft" | "upi"</summary>
    [BsonElement("paymentMode")]
    public string? PaymentMode { get; set; }

    /// <summary>Reference to customer (for filtering). Snapshot stores details.</summary>
    [BsonElement("customerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>Frozen customer details at time of billing — immutable.</summary>
    [BsonElement("customerSnapshot")]
    public CustomerSnapshot CustomerSnapshot { get; set; } = new();

    /// <summary>Frozen company details at time of billing — immutable.</summary>
    [BsonElement("companySnapshot")]
    public CompanySnapshot CompanySnapshot { get; set; } = new();

    /// <summary>All line items embedded in the invoice document.</summary>
    [BsonElement("items")]
    public List<InvoiceItem> Items { get; set; } = new();

    // ── Calculated Totals ─────────────────────────────────────────────────
    /// <summary>Sum of (rate × quantity) for all items, before GST.</summary>
    [BsonElement("subTotal")]
    public decimal SubTotal { get; set; }

    /// <summary>Sum of GST amounts for all items (CGST + SGST combined).</summary>
    [BsonElement("totalGst")]
    public decimal TotalGst { get; set; }

    /// <summary>subTotal + totalGst</summary>
    [BsonElement("grandTotal")]
    public decimal GrandTotal { get; set; }

    /// <summary>Grand total converted to words (e.g. "Six Thousand Three Hundred Only")</summary>
    [BsonElement("totalInWords")]
    public string TotalInWords { get; set; } = string.Empty;

    [BsonElement("remarks")]
    public string? Remarks { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ── Status + Payment Mode Constants ──────────────────────────────────────────

/// <summary>Valid values for Invoice.Status field.</summary>
public static class InvoiceStatus
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Cancelled = "cancelled";
}

/// <summary>Valid values for Invoice.PaymentMode field.</summary>
public static class PaymentModeConstants
{
    public const string Cash = "cash";
    public const string Cheque = "cheque";
    public const string Neft = "neft";
    public const string Upi = "upi";
}

// ── Embedded Sub-Documents ────────────────────────────────────────────────────

/// <summary>
/// Frozen customer details captured at invoice creation time.
/// These values never change even if the customer record is later updated.
/// </summary>
public class CustomerSnapshot
{
    [BsonElement("partyName")]
    public string PartyName { get; set; } = string.Empty;

    [BsonElement("contactPerson")]
    public string? ContactPerson { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("billingAddress")]
    public string? BillingAddress { get; set; }

    [BsonElement("gstin")]
    public string? Gstin { get; set; }
}

/// <summary>
/// Frozen company details captured at invoice creation time.
/// Historical invoices stay accurate even if settings are later changed.
/// </summary>
public class CompanySnapshot
{
    [BsonElement("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("gstin")]
    public string Gstin { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("email")]
    public string? Email { get; set; }

    /// <summary>Cloudinary URL of the digital signature image at time of billing.</summary>
    [BsonElement("signatureUrl")]
    public string? SignatureUrl { get; set; }

    [BsonElement("bankName")]
    public string? BankName { get; set; }

    [BsonElement("bankAccount")]
    public string? BankAccount { get; set; }

    [BsonElement("bankIfsc")]
    public string? BankIfsc { get; set; }
}

/// <summary>
/// A single line item in an invoice.
/// Rate is the ACTUAL price charged (customer-negotiated or base, resolved at billing time).
/// This rate is permanently stored — price changes after billing don't affect old invoices.
/// </summary>
public class InvoiceItem
{
    [BsonElement("productId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("modelNumber")]
    public string ModelNumber { get; set; } = string.Empty;

    /// <summary>HSN code for GST filing.</summary>
    [BsonElement("hsnCode")]
    public string HsnCode { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    /// <summary>Price per unit (resolved from customer pricing or base price).</summary>
    [BsonElement("rate")]
    public decimal Rate { get; set; }

    /// <summary>rate × quantity (before GST)</summary>
    [BsonElement("subTotal")]
    public decimal SubTotal { get; set; }

    /// <summary>GST rate percentage (e.g. 5.0 for 5%)</summary>
    [BsonElement("gstRate")]
    public decimal GstRate { get; set; }

    /// <summary>subTotal × gstRate / 100, rounded to 2 decimal places</summary>
    [BsonElement("gstAmount")]
    public decimal GstAmount { get; set; }

    /// <summary>subTotal + gstAmount</summary>
    [BsonElement("lineTotal")]
    public decimal LineTotal { get; set; }
}
