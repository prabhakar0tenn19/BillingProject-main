using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Dashboard data for the home screen.
///
/// GET /api/v1/dashboard/summary         → KPI cards
/// GET /api/v1/dashboard/monthly-sales   → 12-month chart data
/// GET /api/v1/dashboard/recent-invoices → Latest 10 invoices
/// GET /api/v1/dashboard/top-customers   → Top 5 buyers by revenue
/// </summary>
[Tags("Dashboard")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var summary = await _service.GetSummaryAsync();
            return OkResponse(summary);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("monthly-sales")]
    public async Task<IActionResult> GetMonthlySales([FromQuery] int months = 12)
    {
        try
        {
            var data = await _service.GetMonthlySalesAsync(Math.Clamp(months, 1, 24));
            return OkResponse(data);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("recent-invoices")]
    public async Task<IActionResult> GetRecentInvoices([FromQuery] int count = 10)
    {
        try
        {
            var invoices = await _service.GetRecentInvoicesAsync(Math.Clamp(count, 1, 50));
            return OkResponse(invoices);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("top-customers")]
    public async Task<IActionResult> GetTopCustomers([FromQuery] int count = 5)
    {
        try
        {
            var customers = await _service.GetTopCustomersAsync(Math.Clamp(count, 1, 20));
            return OkResponse(customers);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }
}
