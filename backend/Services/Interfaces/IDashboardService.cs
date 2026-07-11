using BillingSystem.DTOs;

namespace BillingSystem.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync();
    Task<List<MonthlySalesData>> GetMonthlySalesAsync(int months = 12);
    Task<List<InvoiceListResponse>> GetRecentInvoicesAsync(int count = 10);
    Task<List<TopCustomerData>> GetTopCustomersAsync(int count = 5);
}
