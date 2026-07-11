namespace BillingSystem.DTOs;

// ─── Dashboard Response DTOs ──────────────────────────────────────────────────

/// <summary>KPI cards shown on the dashboard home page.</summary>
public record DashboardSummaryResponse(
    decimal TodaySales,
    decimal MonthSales,
    decimal TotalPendingAmount,
    int TotalInvoicesThisMonth,
    int PendingCount,
    int PaidCount,
    int TotalCustomers,
    int TotalProducts
);

/// <summary>One month's data point for the sales bar chart.</summary>
public record MonthlySalesData(
    string Month,        // e.g. "Jul 2024"
    int Year,
    int MonthNumber,
    decimal Sales,
    decimal GstCollected,
    int InvoiceCount
);

/// <summary>Top customer by revenue — shown on dashboard.</summary>
public record TopCustomerData(
    string CustomerId,
    string PartyName,
    decimal TotalPurchased,
    int InvoiceCount
);
