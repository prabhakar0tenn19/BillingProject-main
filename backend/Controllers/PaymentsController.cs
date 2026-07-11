using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Payment status management for invoices.
///
/// GET    /api/v1/payments/pending         → All pending invoices
/// PATCH  /api/v1/invoices/{id}/mark-paid  → Mark as paid (with payment mode)
/// PATCH  /api/v1/invoices/{id}/mark-pending → Revert to pending
/// </summary>
[Tags("Payments")]
public class PaymentsController : BaseApiController
{
    private readonly IInvoiceService _service;

    public PaymentsController(IInvoiceService service) => _service = service;

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        try
        {
            var pending = await _service.GetPendingAsync();
            return OkResponse(pending);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPatch("/api/v1/invoices/{id}/mark-paid")]
    public async Task<IActionResult> MarkPaid(string id, [FromBody] MarkPaidRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Payment mode is required");
        try
        {
            var updated = await _service.MarkPaidAsync(id, request);
            if (!updated) return NotFoundResponse("Invoice not found or already paid");
            return OkResponse(new { id, status = "paid" }, "Invoice marked as paid");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPatch("/api/v1/invoices/{id}/mark-pending")]
    public async Task<IActionResult> MarkPending(string id)
    {
        try
        {
            var updated = await _service.MarkPendingAsync(id);
            if (!updated) return NotFoundResponse("Invoice not found or not in paid status");
            return OkResponse(new { id, status = "pending" }, "Invoice reverted to pending");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }
}
