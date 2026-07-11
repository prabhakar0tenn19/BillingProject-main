using BillingSystem.DTOs;
using BillingSystem.Models;
using Microsoft.AspNetCore.Http;

namespace BillingSystem.Services.Interfaces;

public interface ISettingsService
{
    /// <summary>Get company settings (public-safe response, no nextInvoiceNumber).</summary>
    Task<SettingsResponse> GetAsync();

    /// <summary>Update company settings (name, address, GST, bank, etc.).</summary>
    Task<SettingsResponse> UpdateAsync(UpdateSettingsRequest request);

    /// <summary>Upload digital signature to Cloudinary and save URL in settings.</summary>
    Task<SettingsResponse> UploadSignatureAsync(IFormFile file);

    /// <summary>Delete signature from Cloudinary and clear URL from settings.</summary>
    Task<SettingsResponse> DeleteSignatureAsync();

    /// <summary>
    /// INTERNAL — returns the full Settings model including nextInvoiceNumber.
    /// Used only by InvoiceService to take company snapshot and generate invoice numbers.
    /// </summary>
    Task<Settings> GetInternalAsync();
}
