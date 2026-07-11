using System.ComponentModel.DataAnnotations;

namespace BillingSystem.DTOs;

// ─── Invoice Request DTOs ─────────────────────────────────────────────────────

/// <summary>
/// Request body for creating a new invoice.
/// System auto-resolves prices from customer_pricing (or basePrice fallback).
/// OverrideRate on items allows manual price override if needed.
/// </summary>
public record CreateInvoiceRequest(
    [Required(ErrorMessage = "Customer/Party is required")]
    string CustomerId,

    DateTime InvoiceDate,
    DateTime? DueDate,

    [MinLength(1, ErrorMessage = "At least one item is required")]
    List<CreateInvoiceItemRequest> Items,

    string? Remarks
);

public record CreateInvoiceItemRequest(
    [Required(ErrorMessage = "Product is required")]
    string ProductId,

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    int Quantity,

    /// <summary>
    /// Optional manual price override. If null, system resolves price automatically:
    ///   1. Check customer_pricing for this party
    ///   2. Fall back to product.basePrice
    /// </summary>
    decimal? OverrideRate
);

/// <summary>Filter parameters for invoice list endpoint.</summary>
public class InvoiceFilterRequest
{
    public string? CustomerId { get; set; }
    public string? Status { get; set; }          // pending | paid | cancelled
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Search { get; set; }          // invoice number search
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Mark an invoice as paid.</summary>
public record MarkPaidRequest(
    [Required(ErrorMessage = "Payment mode is required")]
    string PaymentMode,   // cash | cheque | neft | upi

    DateTime? PaidAt      // defaults to now if null
);

// ─── Invoice Response DTOs ────────────────────────────────────────────────────

/// <summary>Full invoice detail — returned when viewing/generating a single invoice.</summary>
public record InvoiceResponse(
    string Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string Status,
    DateTime? PaidAt,
    string? PaymentMode,
    string CustomerId,
    CustomerSnapshotDto CustomerSnapshot,
    CompanySnapshotDto CompanySnapshot,
    List<InvoiceItemDto> Items,
    decimal SubTotal,
    decimal TotalGst,
    decimal GrandTotal,
    string TotalInWords,
    string? Remarks,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Lightweight invoice — used in list views and dashboard.</summary>
public record InvoiceListResponse(
    string Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string PartyName,
    string? PartyGstin,
    decimal SubTotal,
    decimal TotalGst,
    decimal GrandTotal,
    string Status,
    DateTime? PaidAt,
    string? PaymentMode,
    DateTime CreatedAt
);

public record CustomerSnapshotDto(
    string PartyName,
    string? ContactPerson,
    string? Phone,
    string? BillingAddress,
    string? Gstin
);

public record CompanySnapshotDto(
    string CompanyName,
    string Address,
    string Gstin,
    string Phone,
    string? Email,
    string? SignatureUrl,
    string? BankName,
    string? BankAccount,
    string? BankIfsc
);

public record InvoiceItemDto(
    string ProductId,
    string ProductName,
    string ModelNumber,
    string HsnCode,
    int Quantity,
    decimal Rate,
    decimal SubTotal,
    decimal GstRate,
    decimal GstAmount,
    decimal LineTotal
);
