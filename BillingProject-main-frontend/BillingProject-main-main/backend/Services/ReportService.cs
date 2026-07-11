using BillingSystem.DTOs;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using BillingSystem.Services.Interfaces;
using MongoDB.Driver;

namespace BillingSystem.Services;

/// <summary>
/// Business reporting service.
/// All reports filter on paid+pending invoices (excludes cancelled).
/// Useful for GST filing, customer analysis, and product performance.
/// </summary>
public class ReportService : IReportService
{
    private readonly MongoDbContext _db;

    public ReportService(MongoDbContext db) => _db = db;

    public async Task<SalesReportResponse> GetSalesReportAsync(DateTime from, DateTime to)
    {
        var invoices = await GetInvoicesInRangeAsync(from, to);

        return new SalesReportResponse(
            TotalSales:          invoices.Sum(i => i.GrandTotal),
            TotalGstCollected:   invoices.Sum(i => i.TotalGst),
            TotalTaxableAmount:  invoices.Sum(i => i.SubTotal),
            InvoiceCount:        invoices.Count,
            PaidCount:           invoices.Count(i => i.Status == InvoiceStatus.Paid),
            PendingCount:        invoices.Count(i => i.Status == InvoiceStatus.Pending),
            FromDate:            from,
            ToDate:              to
        );
    }

    public async Task<List<CustomerSalesData>> GetCustomerSalesAsync(DateTime from, DateTime to)
    {
        var invoices = await GetInvoicesInRangeAsync(from, to);

        return invoices
            .GroupBy(i => i.CustomerId)
            .Select(g => new CustomerSalesData(
                CustomerId:  g.Key,
                PartyName:   g.First().CustomerSnapshot.PartyName,
                Gstin:       g.First().CustomerSnapshot.Gstin,
                TotalSales:  g.Sum(i => i.GrandTotal),
                TotalGst:    g.Sum(i => i.TotalGst),
                InvoiceCount: g.Count()
            ))
            .OrderByDescending(c => c.TotalSales)
            .ToList();
    }

    public async Task<List<ProductSalesData>> GetProductSalesAsync(DateTime from, DateTime to)
    {
        var invoices = await GetInvoicesInRangeAsync(from, to);

        // Flatten all items across all invoices
        var allItems = invoices.SelectMany(i => i.Items);

        return allItems
            .GroupBy(item => item.ProductId)
            .Select(g =>
            {
                var first = g.First();
                return new ProductSalesData(
                    ProductId:         first.ProductId,
                    ProductName:       first.ProductName,
                    ModelNumber:       first.ModelNumber,
                    CategoryName:      string.Empty,   // enriched if needed
                    HsnCode:           first.HsnCode,
                    TotalQuantitySold: g.Sum(i => i.Quantity),
                    TotalRevenue:      g.Sum(i => i.LineTotal),
                    TotalGst:          g.Sum(i => i.GstAmount)
                );
            })
            .OrderByDescending(p => p.TotalRevenue)
            .ToList();
    }

    public async Task<List<CustomerSalesData>> GetTopBuyersAsync(int count = 10)
    {
        var invoices = await _db.Invoices
            .Find(i => i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        return invoices
            .GroupBy(i => i.CustomerId)
            .Select(g => new CustomerSalesData(
                CustomerId:  g.Key,
                PartyName:   g.First().CustomerSnapshot.PartyName,
                Gstin:       g.First().CustomerSnapshot.Gstin,
                TotalSales:  g.Sum(i => i.GrandTotal),
                TotalGst:    g.Sum(i => i.TotalGst),
                InvoiceCount: g.Count()
            ))
            .OrderByDescending(c => c.TotalSales)
            .Take(count)
            .ToList();
    }

    public async Task<GstSummaryResponse> GetGstSummaryAsync(DateTime from, DateTime to)
    {
        var invoices = await GetInvoicesInRangeAsync(from, to);

        decimal taxable   = invoices.Sum(i => i.SubTotal);
        decimal totalGst  = invoices.Sum(i => i.TotalGst);
        decimal cgst      = Math.Round(totalGst / 2, 2);
        decimal sgst      = totalGst - cgst;   // handles odd paise rounding

        return new GstSummaryResponse(
            TaxableAmount:   taxable,
            CgstCollected:   cgst,
            SgstCollected:   sgst,
            TotalGst:        totalGst,
            GrandTotal:      taxable + totalGst,
            InvoiceCount:    invoices.Count,
            FromDate:        from,
            ToDate:          to
        );
    }

    // ─── Private Helper ───────────────────────────────────────────────────────

    private async Task<List<Invoice>> GetInvoicesInRangeAsync(DateTime from, DateTime to)
    {
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Ne(i => i.Status, InvoiceStatus.Cancelled),
            Builders<Invoice>.Filter.Gte(i => i.InvoiceDate, from.Date),
            Builders<Invoice>.Filter.Lte(i => i.InvoiceDate, to.Date.AddDays(1).AddTicks(-1))
        );

        return await _db.Invoices.Find(filter).ToListAsync();
    }
}
