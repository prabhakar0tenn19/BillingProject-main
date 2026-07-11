using BillingSystem.DTOs;

namespace BillingSystem.Services.Interfaces;

public interface IReportService
{
    Task<SalesReportResponse> GetSalesReportAsync(DateTime from, DateTime to);
    Task<List<CustomerSalesData>> GetCustomerSalesAsync(DateTime from, DateTime to);
    Task<List<ProductSalesData>> GetProductSalesAsync(DateTime from, DateTime to);
    Task<List<CustomerSalesData>> GetTopBuyersAsync(int count = 10);
    Task<GstSummaryResponse> GetGstSummaryAsync(DateTime from, DateTime to);
}
