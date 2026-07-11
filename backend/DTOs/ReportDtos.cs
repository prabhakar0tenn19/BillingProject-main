namespace BillingSystem.DTOs;

// ─── Report Response DTOs ─────────────────────────────────────────────────────

/// <summary>Overall sales summary for a date range.</summary>
public record SalesReportResponse(
    decimal TotalSales,
    decimal TotalGstCollected,
    decimal TotalTaxableAmount,
    int InvoiceCount,
    int PaidCount,
    int PendingCount,
    DateTime FromDate,
    DateTime ToDate
);

/// <summary>Sales breakdown per customer.</summary>
public record CustomerSalesData(
    string CustomerId,
    string PartyName,
    string? Gstin,
    decimal TotalSales,
    decimal TotalGst,
    int InvoiceCount
);

/// <summary>Sales breakdown per product (quantity + revenue).</summary>
public record ProductSalesData(
    string ProductId,
    string ProductName,
    string ModelNumber,
    string CategoryName,
    string HsnCode,
    int TotalQuantitySold,
    decimal TotalRevenue,
    decimal TotalGst
);

/// <summary>GST summary for a period — useful for GST return filing.</summary>
public record GstSummaryResponse(
    decimal TaxableAmount,
    decimal CgstCollected,   // CGST = totalGst / 2
    decimal SgstCollected,   // SGST = totalGst / 2
    decimal TotalGst,
    decimal GrandTotal,
    int InvoiceCount,
    DateTime FromDate,
    DateTime ToDate
);
