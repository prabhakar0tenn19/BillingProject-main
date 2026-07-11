using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Invoice management — create, list, view, update, cancel, PDF download.
///
/// GET    /api/v1/invoices              → List (filter: customerId, status, dateFrom, dateTo, search, page)
/// POST   /api/v1/invoices              → Create invoice (full billing flow)
/// GET    /api/v1/invoices/{id}         → Get full invoice detail
/// PUT    /api/v1/invoices/{id}         → Update invoice (pending only)
/// DELETE /api/v1/invoices/{id}         → Cancel invoice
/// GET    /api/v1/invoices/{id}/pdf     → Download PDF
/// </summary>
[Tags("Invoices")]
public class InvoicesController : BaseApiController
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPdfService _pdfService;

    public InvoicesController(IInvoiceService invoiceService, IPdfService pdfService)
    {
        _invoiceService = invoiceService;
        _pdfService     = pdfService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] InvoiceFilterRequest filter)
    {
        try
        {
            var result = await _invoiceService.GetAllAsync(filter);
            return OkResponse(result);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice is null) return NotFoundResponse("Invoice not found");
            return OkResponse(invoice);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var invoice = await _invoiceService.CreateAsync(request);
            return CreatedResponse(invoice, $"Invoice {invoice.InvoiceNumber} created successfully");
        }
        catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateInvoiceRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var updated = await _invoiceService.UpdateAsync(id, request);
            if (updated is null) return NotFoundResponse("Invoice not found");
            return OkResponse(updated, "Invoice updated");
        }
        catch (InvalidOperationException ex) { return BadRequestResponse(ex.Message); }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(string id)
    {
        try
        {
            var cancelled = await _invoiceService.CancelAsync(id);
            if (!cancelled) return NotFoundResponse("Invoice not found");
            return OkResponse(new { id }, "Invoice cancelled");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    /// <summary>
    /// Generate and stream invoice PDF for download/print.
    /// Returns application/pdf — browser opens print dialog or download prompt.
    /// </summary>
    [HttpGet("{id}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetPdf(string id)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice is null) return NotFoundResponse("Invoice not found");

            var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoice);
            var fileName = $"{invoice.InvoiceNumber}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }
}
