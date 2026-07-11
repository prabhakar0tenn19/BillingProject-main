using BillingSystem.DTOs;

namespace BillingSystem.Services.Interfaces;

public interface IPdfService
{
    /// <summary>
    /// Generates a GST-compliant tax invoice PDF using QuestPDF.
    ///
    /// PDF includes:
    ///   - Company header with GSTIN
    ///   - "TAX INVOICE" heading
    ///   - Invoice number, date, due date
    ///   - Customer details (party name, GSTIN, address)
    ///   - Items table with HSN codes, quantity, rate, taxable amount, CGST 2.5%, SGST 2.5%, total
    ///   - Grand total + amount in words
    ///   - Bank details
    ///   - Digital signature (fetched from Cloudinary, embedded as bytes)
    ///   - "Authorized Signatory" footer
    ///
    /// Returns raw PDF bytes — caller is responsible for streaming to response.
    /// </summary>
    Task<byte[]> GenerateInvoicePdfAsync(InvoiceResponse invoice);
}
