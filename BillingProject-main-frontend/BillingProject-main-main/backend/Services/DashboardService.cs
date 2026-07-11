using BillingSystem.DTOs;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using BillingSystem.Services.Interfaces;
using MongoDB.Driver;

namespace BillingSystem.Services;

/// <summary>
/// Dashboard aggregation service.
/// Provides KPI data, charts, and recent activity for the home screen.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly MongoDbContext _db;

    public DashboardService(MongoDbContext db) => _db = db;

    public async Task<DashboardSummaryResponse> GetSummaryAsync()
    {
        var now         = DateTime.UtcNow;
        var todayStart  = now.Date;
        var monthStart  = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var filterBuilder = Builders<Invoice>.Filter;
        var notCancelled  = filterBuilder.Ne(i => i.Status, InvoiceStatus.Cancelled);

        // Today's sales (paid invoices created today)
        var todayFilter = filterBuilder.And(
            notCancelled,
            filterBuilder.Gte(i => i.InvoiceDate, todayStart),
            filterBuilder.Lt(i => i.InvoiceDate,  todayStart.AddDays(1))
        );
        var todayInvoices = await _db.Invoices.Find(todayFilter).ToListAsync();
        decimal todaySales = todayInvoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.GrandTotal);

        // Month sales
        var monthFilter = filterBuilder.And(
            notCancelled,
            filterBuilder.Gte(i => i.InvoiceDate, monthStart)
        );
        var monthInvoices = await _db.Invoices.Find(monthFilter).ToListAsync();
        decimal monthSales  = monthInvoices.Sum(i => i.GrandTotal);
        int thisMonthCount  = monthInvoices.Count;
        int paidCount       = monthInvoices.Count(i => i.Status == InvoiceStatus.Paid);

        // All pending invoices (any time)
        var allPending = await _db.Invoices
            .Find(i => i.Status == InvoiceStatus.Pending)
            .ToListAsync();
        decimal pendingAmount = allPending.Sum(i => i.GrandTotal);

        // Customer and product counts
        var customerCount = await _db.Customers.CountDocumentsAsync(c => c.IsActive);
        var productCount  = await _db.Products.CountDocumentsAsync(p => p.IsActive);

        return new DashboardSummaryResponse(
            TodaySales:             todaySales,
            MonthSales:             monthSales,
            TotalPendingAmount:     pendingAmount,
            TotalInvoicesThisMonth: thisMonthCount,
            PendingCount:           allPending.Count,
            PaidCount:              paidCount,
            TotalCustomers:         (int)customerCount,
            TotalProducts:          (int)productCount
        );
    }

    public async Task<List<MonthlySalesData>> GetMonthlySalesAsync(int months = 12)
    {
        var startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(months - 1));

        var invoices = await _db.Invoices
            .Find(i => i.InvoiceDate >= startDate && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        // Group by year-month
        var grouped = invoices
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new MonthlySalesData(
                Month:        $"{g.Key.Year}-{g.Key.Month:D2}",
                Year:         g.Key.Year,
                MonthNumber:  g.Key.Month,
                Sales:        g.Sum(i => i.GrandTotal),
                GstCollected: g.Sum(i => i.TotalGst),
                InvoiceCount: g.Count()
            ))
            .OrderBy(m => m.Year)
            .ThenBy(m => m.MonthNumber)
            .ToList();

        // Fill missing months with zeros
        var result = new List<MonthlySalesData>();
        for (int i = 0; i < months; i++)
        {
            var date = startDate.AddMonths(i);
            var found = grouped.FirstOrDefault(g => g.Year == date.Year && g.MonthNumber == date.Month);
            result.Add(found ?? new MonthlySalesData(
                Month:        $"{date.Year}-{date.Month:D2}",
                Year:         date.Year,
                MonthNumber:  date.Month,
                Sales:        0,
                GstCollected: 0,
                InvoiceCount: 0
            ));
        }

        return result;
    }

    public async Task<List<InvoiceListResponse>> GetRecentInvoicesAsync(int count = 10)
    {
        var invoices = await _db.Invoices
            .Find(i => i.Status != InvoiceStatus.Cancelled)
            .SortByDescending(i => i.CreatedAt)
            .Limit(count)
            .ToListAsync();

        return invoices.Select(i => new InvoiceListResponse(
            Id:            i.Id!,
            InvoiceNumber: i.InvoiceNumber,
            InvoiceDate:   i.InvoiceDate,
            DueDate:       i.DueDate,
            PartyName:     i.CustomerSnapshot.PartyName,
            PartyGstin:    i.CustomerSnapshot.Gstin,
            SubTotal:      i.SubTotal,
            TotalGst:      i.TotalGst,
            GrandTotal:    i.GrandTotal,
            Status:        i.Status,
            PaidAt:        i.PaidAt,
            PaymentMode:   i.PaymentMode,
            CreatedAt:     i.CreatedAt
        )).ToList();
    }

    public async Task<List<TopCustomerData>> GetTopCustomersAsync(int count = 5)
    {
        var invoices = await _db.Invoices
            .Find(i => i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        return invoices
            .GroupBy(i => i.CustomerId)
            .Select(g => new TopCustomerData(
                CustomerId:      g.Key,
                PartyName:       g.First().CustomerSnapshot.PartyName,
                TotalPurchased:  g.Sum(i => i.GrandTotal),
                InvoiceCount:    g.Count()
            ))
            .OrderByDescending(c => c.TotalPurchased)
            .Take(count)
            .ToList();
    }
}
