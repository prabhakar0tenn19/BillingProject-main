using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Business reports with date range filtering.
///
/// GET /api/v1/reports/sales           → Overall sales summary
/// GET /api/v1/reports/customer-sales  → Sales by customer
/// GET /api/v1/reports/product-sales   → Sales by product
/// GET /api/v1/reports/top-buyers      → Top buyers ranked
/// GET /api/v1/reports/gst-summary     → GST collected (for filing)
/// </summary>
[Tags("Reports")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _service;

    public ReportsController(IReportService service) => _service = service;

    [HttpGet("sales")]
    public async Task<IActionResult> GetSales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var fromDate = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var toDate   = to   ?? DateTime.UtcNow;
            var report   = await _service.GetSalesReportAsync(fromDate, toDate);
            return OkResponse(report);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("customer-sales")]
    public async Task<IActionResult> GetCustomerSales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var fromDate = from ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
            var toDate   = to   ?? DateTime.UtcNow;
            var data     = await _service.GetCustomerSalesAsync(fromDate, toDate);
            return OkResponse(data);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("product-sales")]
    public async Task<IActionResult> GetProductSales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var fromDate = from ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
            var toDate   = to   ?? DateTime.UtcNow;
            var data     = await _service.GetProductSalesAsync(fromDate, toDate);
            return OkResponse(data);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("top-buyers")]
    public async Task<IActionResult> GetTopBuyers([FromQuery] int count = 10)
    {
        try
        {
            var data = await _service.GetTopBuyersAsync(Math.Clamp(count, 1, 50));
            return OkResponse(data);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("gst-summary")]
    public async Task<IActionResult> GetGstSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var fromDate = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var toDate   = to   ?? DateTime.UtcNow;
            var data     = await _service.GetGstSummaryAsync(fromDate, toDate);
            return OkResponse(data);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }
}
