using BillingSystem.DTOs;

namespace BillingSystem.Services.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListResponse>> GetAllAsync(InvoiceFilterRequest filter);
    Task<InvoiceResponse?> GetByIdAsync(string id);

    /// <summary>
    /// Create a new invoice.
    /// Responsibilities:
    ///   1. Atomically generate invoice number (MongoDB $inc)
    ///   2. Capture company + customer snapshots
    ///   3. Resolve prices (customer override → basePrice fallback)
    ///   4. Calculate GST per line, round correctly
    ///   5. Calculate totals + amount in words
    ///   6. Persist and return full invoice
    /// </summary>
    Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request);

    /// <summary>Update invoice (only allowed if status = pending).</summary>
    Task<InvoiceResponse?> UpdateAsync(string id, CreateInvoiceRequest request);

    /// <summary>Cancel invoice (soft delete — sets status = cancelled).</summary>
    Task<bool> CancelAsync(string id);

    Task<bool> MarkPaidAsync(string id, MarkPaidRequest request);
    Task<bool> MarkPendingAsync(string id);

    Task<List<InvoiceListResponse>> GetPendingAsync();
}
