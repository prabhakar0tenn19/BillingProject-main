using BillingSystem.DTOs;
using BillingSystem.Helpers;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using BillingSystem.Services.Interfaces;
using MongoDB.Driver;

namespace BillingSystem.Services;

/// <summary>
/// Core billing service — creates and manages invoices.
///
/// CreateAsync() flow:
///   1. Validate customer exists
///   2. Atomically generate invoice number (MongoDB $inc — thread-safe)
///   3. Capture company snapshot from settings
///   4. Capture customer snapshot
///   5. For each item:
///      a. Validate product exists
///      b. Resolve effective price (customer override → basePrice fallback)
///      c. Calculate: subTotal = rate × qty
///      d. Calculate: gstAmount = round(subTotal × gstRate / 100, 2)
///      e. Calculate: lineTotal = subTotal + gstAmount
///   6. Sum totals
///   7. Convert grand total to words
///   8. Insert invoice document
///   9. Return full InvoiceResponse
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly MongoDbContext _db;
    private readonly IProductService _productService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        MongoDbContext db,
        IProductService productService,
        ISettingsService settingsService,
        ILogger<InvoiceService> logger)
    {
        _db              = db;
        _productService  = productService;
        _settingsService = settingsService;
        _logger          = logger;
    }

    // ─── Read Operations ──────────────────────────────────────────────────────

    public async Task<PagedResult<InvoiceListResponse>> GetAllAsync(InvoiceFilterRequest filter)
    {
        var filterBuilder = Builders<Invoice>.Filter;
        var mongoFilter   = filterBuilder.Ne(i => i.Status, InvoiceStatus.Cancelled);

        if (!string.IsNullOrEmpty(filter.CustomerId))
            mongoFilter &= filterBuilder.Eq(i => i.CustomerId, filter.CustomerId);

        if (!string.IsNullOrEmpty(filter.Status))
            mongoFilter &= filterBuilder.Eq(i => i.Status, filter.Status);

        if (filter.DateFrom.HasValue)
            mongoFilter &= filterBuilder.Gte(i => i.InvoiceDate, filter.DateFrom.Value.Date);

        if (filter.DateTo.HasValue)
            mongoFilter &= filterBuilder.Lte(i => i.InvoiceDate, filter.DateTo.Value.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var regex = new MongoDB.Bson.BsonRegularExpression(filter.Search, "i");
            mongoFilter &= filterBuilder.Or(
                filterBuilder.Regex(i => i.InvoiceNumber, regex),
                filterBuilder.Regex("customerSnapshot.partyName", regex)
            );
        }

        var total = await _db.Invoices.CountDocumentsAsync(mongoFilter);

        var invoices = await _db.Invoices
            .Find(mongoFilter)
            .SortByDescending(i => i.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Limit(filter.PageSize)
            .ToListAsync();

        return new PagedResult<InvoiceListResponse>
        {
            Data     = invoices.Select(ToListDto).ToList(),
            Total    = (int)total,
            Page     = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<InvoiceResponse?> GetByIdAsync(string id)
    {
        var invoice = await _db.Invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
        return invoice is null ? null : ToDetailDto(invoice);
    }

    public async Task<List<InvoiceListResponse>> GetPendingAsync()
    {
        var invoices = await _db.Invoices
            .Find(i => i.Status == InvoiceStatus.Pending)
            .SortByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices.Select(ToListDto).ToList();
    }

    // ─── Create Invoice (Main Billing Flow) ───────────────────────────────────

    public async Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request)
    {
        // Step 1: Validate customer
        var customer = await _db.Customers
            .Find(c => c.Id == request.CustomerId && c.IsActive)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Customer '{request.CustomerId}' not found or inactive");

        // Step 2: Get company settings (for snapshot + GST rate + invoice number)
        var settings = await _settingsService.GetInternalAsync();

        // Step 3: Generate unique invoice number (ATOMIC — MongoDB $inc)
        var invoiceNumber = await GenerateInvoiceNumberAsync(settings);

        // Step 4: Load customer's pricing overrides (for price resolution)
        var customerOverrides = await _db.CustomerPricing
            .Find(p => p.CustomerId == request.CustomerId && p.IsActive)
            .ToListAsync();
        var overrideLookup = customerOverrides.ToDictionary(o => o.ProductId, o => o.NegotiatedPrice);

        // Step 5: Build invoice items with resolved prices and GST
        var items = new List<InvoiceItem>();
        foreach (var itemReq in request.Items)
        {
            var product = await _productService.GetInternalAsync(itemReq.ProductId)
                ?? throw new KeyNotFoundException($"Product '{itemReq.ProductId}' not found");

            // Resolve effective rate:
            //   Manual override (if provided) > Customer pricing > Base price
            decimal rate;
            if (itemReq.OverrideRate.HasValue && itemReq.OverrideRate.Value > 0)
            {
                rate = itemReq.OverrideRate.Value;
            }
            else if (overrideLookup.TryGetValue(product.Id!, out var customPrice))
            {
                rate = customPrice;
            }
            else
            {
                rate = product.BasePrice;
            }

            decimal subTotal  = rate * itemReq.Quantity;
            decimal gstAmount = Math.Round(subTotal * settings.GstRate / 100, 2);
            decimal lineTotal = subTotal + gstAmount;

            items.Add(new InvoiceItem
            {
                ProductId   = product.Id!,
                ProductName = product.Name,
                ModelNumber = product.ModelNumber,
                HsnCode     = product.HsnCode,
                Quantity    = itemReq.Quantity,
                Rate        = rate,
                SubTotal    = subTotal,
                GstRate     = settings.GstRate,
                GstAmount   = gstAmount,
                LineTotal   = lineTotal
            });
        }

        // Step 6: Calculate totals
        decimal totalSubTotal = items.Sum(i => i.SubTotal);
        decimal totalGst      = items.Sum(i => i.GstAmount);
        decimal grandTotal    = totalSubTotal + totalGst;

        // Step 7: Build the invoice document with frozen snapshots
        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate   = request.InvoiceDate.Date == DateTime.MinValue.Date
                            ? DateTime.UtcNow
                            : request.InvoiceDate,
            DueDate       = request.DueDate,
            Status        = InvoiceStatus.Pending,
            CustomerId    = customer.Id!,

            // Frozen company state at billing time
            CompanySnapshot = new CompanySnapshot
            {
                CompanyName = settings.CompanyName,
                Address     = settings.CompanyAddress,
                Gstin       = settings.Gstin,
                Phone       = settings.CompanyPhone,
                Email       = settings.CompanyEmail,
                SignatureUrl = settings.SignatureCloudinaryUrl,
                BankName    = settings.BankName,
                BankAccount = settings.BankAccount,
                BankIfsc    = settings.BankIfsc
            },

            // Frozen customer state at billing time
            CustomerSnapshot = new CustomerSnapshot
            {
                PartyName      = customer.PartyName,
                ContactPerson  = customer.ContactPerson,
                Phone          = customer.Phone,
                BillingAddress = customer.BillingAddress,
                Gstin          = customer.Gstin
            },

            Items        = items,
            SubTotal     = totalSubTotal,
            TotalGst     = totalGst,
            GrandTotal   = grandTotal,
            TotalInWords = NumberToWords.Convert(grandTotal),
            Remarks      = request.Remarks?.Trim(),
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        await _db.Invoices.InsertOneAsync(invoice);
        _logger.LogInformation("Invoice created: {Number} for {Party}, Total: {Total}",
            invoiceNumber, customer.PartyName, grandTotal);

        return ToDetailDto(invoice);
    }

    // ─── Update Invoice ───────────────────────────────────────────────────────

    public async Task<InvoiceResponse?> UpdateAsync(string id, CreateInvoiceRequest request)
    {
        // Can only update pending invoices
        var existing = await _db.Invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
        if (existing is null) return null;
        if (existing.Status != InvoiceStatus.Pending)
            throw new InvalidOperationException("Only pending invoices can be updated");

        // Re-run the creation logic to rebuild items/totals
        // but preserve the original invoice number and creation time
        var rebuilt = await CreateAsync(request);

        // Delete the rebuilt one (we used CreateAsync as a helper)
        await _db.Invoices.DeleteOneAsync(i => i.Id == rebuilt.Id);

        // Update the original
        var customer = await _db.Customers.Find(c => c.Id == request.CustomerId).FirstOrDefaultAsync()!;
        var settings = await _settingsService.GetInternalAsync();

        // Rebuild items same as CreateAsync
        var customerOverrides = await _db.CustomerPricing
            .Find(p => p.CustomerId == request.CustomerId && p.IsActive)
            .ToListAsync();
        var overrideLookup = customerOverrides.ToDictionary(o => o.ProductId, o => o.NegotiatedPrice);

        var items = new List<InvoiceItem>();
        foreach (var itemReq in request.Items)
        {
            var product = await _productService.GetInternalAsync(itemReq.ProductId)!;
            decimal rate = itemReq.OverrideRate ?? (overrideLookup.TryGetValue(product!.Id!, out var cp) ? cp : product.BasePrice);
            decimal sub  = rate * itemReq.Quantity;
            decimal gst  = Math.Round(sub * settings.GstRate / 100, 2);

            items.Add(new InvoiceItem
            {
                ProductId   = product!.Id!,
                ProductName = product.Name,
                ModelNumber = product.ModelNumber,
                HsnCode     = product.HsnCode,
                Quantity    = itemReq.Quantity,
                Rate        = rate,
                SubTotal    = sub,
                GstRate     = settings.GstRate,
                GstAmount   = gst,
                LineTotal   = sub + gst
            });
        }

        decimal subTotal   = items.Sum(i => i.SubTotal);
        decimal totalGst   = items.Sum(i => i.GstAmount);
        decimal grandTotal = subTotal + totalGst;

        var update = Builders<Invoice>.Update
            .Set(i => i.Items,        items)
            .Set(i => i.SubTotal,     subTotal)
            .Set(i => i.TotalGst,     totalGst)
            .Set(i => i.GrandTotal,   grandTotal)
            .Set(i => i.TotalInWords, NumberToWords.Convert(grandTotal))
            .Set(i => i.InvoiceDate,  request.InvoiceDate)
            .Set(i => i.DueDate,      request.DueDate)
            .Set(i => i.Remarks,      request.Remarks?.Trim())
            .Set(i => i.UpdatedAt,    DateTime.UtcNow);

        var result = await _db.Invoices.FindOneAndUpdateAsync<Invoice, Invoice>(
            i => i.Id == id,
            update,
            new FindOneAndUpdateOptions<Invoice, Invoice> { ReturnDocument = ReturnDocument.After }
        );

        return result is null ? null : ToDetailDto(result);
    }

    // ─── Status Operations ────────────────────────────────────────────────────

    public async Task<bool> CancelAsync(string id)
    {
        var result = await _db.Invoices.UpdateOneAsync(
            i => i.Id == id,
            Builders<Invoice>.Update
                .Set(i => i.Status,    InvoiceStatus.Cancelled)
                .Set(i => i.UpdatedAt, DateTime.UtcNow)
        );

        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkPaidAsync(string id, MarkPaidRequest request)
    {
        var result = await _db.Invoices.UpdateOneAsync(
            i => i.Id == id && i.Status == InvoiceStatus.Pending,
            Builders<Invoice>.Update
                .Set(i => i.Status,      InvoiceStatus.Paid)
                .Set(i => i.PaymentMode, request.PaymentMode)
                .Set(i => i.PaidAt,      request.PaidAt ?? DateTime.UtcNow)
                .Set(i => i.UpdatedAt,   DateTime.UtcNow)
        );

        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkPendingAsync(string id)
    {
        var result = await _db.Invoices.UpdateOneAsync(
            i => i.Id == id && i.Status == InvoiceStatus.Paid,
            Builders<Invoice>.Update
                .Set(i => i.Status,      InvoiceStatus.Pending)
                .Set(i => i.PaymentMode, (string?)null)
                .Set(i => i.PaidAt,      (DateTime?)null)
                .Set(i => i.UpdatedAt,   DateTime.UtcNow)
        );

        return result.ModifiedCount > 0;
    }

    // ─── Invoice Number Generation (ATOMIC) ───────────────────────────────────

    /// <summary>
    /// Generates a unique invoice number using MongoDB's atomic $inc operator.
    /// Returns BEFORE value (current counter), then increments.
    /// Thread-safe: multiple concurrent requests will never get the same number.
    /// Format: INV-2024-0001
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync(Settings settings)
    {
        // findOneAndUpdate with $inc is atomic in MongoDB
        var updated = await _db.Settings.FindOneAndUpdateAsync<Settings, Settings>(
            _ => true,
            Builders<Settings>.Update.Inc(s => s.NextInvoiceNumber, 1),
            new FindOneAndUpdateOptions<Settings, Settings> { ReturnDocument = ReturnDocument.Before }
        );

        int number = updated?.NextInvoiceNumber ?? 1;
        return $"{settings.InvoicePrefix}-{DateTime.UtcNow.Year}-{number:D4}";
    }

    // ─── DTO Mapping ──────────────────────────────────────────────────────────

    private static InvoiceListResponse ToListDto(Invoice i) => new(
        Id:           i.Id!,
        InvoiceNumber: i.InvoiceNumber,
        InvoiceDate:  i.InvoiceDate,
        DueDate:      i.DueDate,
        PartyName:    i.CustomerSnapshot.PartyName,
        PartyGstin:   i.CustomerSnapshot.Gstin,
        SubTotal:     i.SubTotal,
        TotalGst:     i.TotalGst,
        GrandTotal:   i.GrandTotal,
        Status:       i.Status,
        PaidAt:       i.PaidAt,
        PaymentMode:  i.PaymentMode,
        CreatedAt:    i.CreatedAt
    );

    private static InvoiceResponse ToDetailDto(Invoice i) => new(
        Id:            i.Id!,
        InvoiceNumber: i.InvoiceNumber,
        InvoiceDate:   i.InvoiceDate,
        DueDate:       i.DueDate,
        Status:        i.Status,
        PaidAt:        i.PaidAt,
        PaymentMode:   i.PaymentMode,
        CustomerId:    i.CustomerId,
        CustomerSnapshot: new CustomerSnapshotDto(
            i.CustomerSnapshot.PartyName,
            i.CustomerSnapshot.ContactPerson,
            i.CustomerSnapshot.Phone,
            i.CustomerSnapshot.BillingAddress,
            i.CustomerSnapshot.Gstin
        ),
        CompanySnapshot: new CompanySnapshotDto(
            i.CompanySnapshot.CompanyName,
            i.CompanySnapshot.Address,
            i.CompanySnapshot.Gstin,
            i.CompanySnapshot.Phone,
            i.CompanySnapshot.Email,
            i.CompanySnapshot.SignatureUrl,
            i.CompanySnapshot.BankName,
            i.CompanySnapshot.BankAccount,
            i.CompanySnapshot.BankIfsc
        ),
        Items: i.Items.Select(item => new InvoiceItemDto(
            item.ProductId,
            item.ProductName,
            item.ModelNumber,
            item.HsnCode,
            item.Quantity,
            item.Rate,
            item.SubTotal,
            item.GstRate,
            item.GstAmount,
            item.LineTotal
        )).ToList(),
        SubTotal:     i.SubTotal,
        TotalGst:     i.TotalGst,
        GrandTotal:   i.GrandTotal,
        TotalInWords: i.TotalInWords,
        Remarks:      i.Remarks,
        CreatedAt:    i.CreatedAt,
        UpdatedAt:    i.UpdatedAt
    );
}
