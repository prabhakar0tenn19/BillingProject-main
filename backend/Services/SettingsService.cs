using BillingSystem.DTOs;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using BillingSystem.Services.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MongoDB.Driver;

namespace BillingSystem.Services;

/// <summary>
/// Manages company-wide settings including digital signature upload.
///
/// Signature upload flow:
///   1. Receive IFormFile from controller
///   2. Upload to Cloudinary (folder: billing_signatures/)
///   3. If a previous signature existed, delete it from Cloudinary first
///   4. Save new Cloudinary URL + publicId to settings
///   5. Return updated settings
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly MongoDbContext _db;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(MongoDbContext db, IConfiguration config, ILogger<SettingsService> logger)
    {
        _db     = db;
        _logger = logger;

        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public async Task<SettingsResponse> GetAsync()
    {
        var settings = await GetOrCreateSettingsAsync();
        return ToDto(settings);
    }

    public async Task<SettingsResponse> UpdateAsync(UpdateSettingsRequest request)
    {
        var update = Builders<Settings>.Update
            .Set(s => s.CompanyName,    request.CompanyName.Trim())
            .Set(s => s.CompanyAddress, request.CompanyAddress.Trim())
            .Set(s => s.CompanyPhone,   request.CompanyPhone.Trim())
            .Set(s => s.CompanyEmail,   request.CompanyEmail.Trim())
            .Set(s => s.Gstin,          request.Gstin.Trim().ToUpperInvariant())
            .Set(s => s.BankName,       request.BankName.Trim())
            .Set(s => s.BankAccount,    request.BankAccount.Trim())
            .Set(s => s.BankIfsc,       request.BankIfsc.Trim().ToUpperInvariant())
            .Set(s => s.InvoicePrefix,  request.InvoicePrefix.Trim().ToUpperInvariant())
            .Set(s => s.GstRate,        request.GstRate)
            .Set(s => s.UpdatedAt,      DateTime.UtcNow);

        var result = await _db.Settings.FindOneAndUpdateAsync<Settings, Settings>(
            _ => true,   // single settings document
            update,
            new FindOneAndUpdateOptions<Settings, Settings> { ReturnDocument = ReturnDocument.After }
        );

        return ToDto(result);
    }

    public async Task<SettingsResponse> UploadSignatureAsync(IFormFile file)
    {
        var settings = await GetOrCreateSettingsAsync();

        // Delete old signature from Cloudinary if it exists
        if (!string.IsNullOrEmpty(settings.SignaturePublicId))
        {
            await _cloudinary.DestroyAsync(new DeletionParams(settings.SignaturePublicId));
            _logger.LogInformation("Deleted old signature: {PublicId}", settings.SignaturePublicId);
        }

        // Upload new signature to Cloudinary
        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File           = new FileDescription(file.FileName, stream),
            Folder         = "billing_signatures",
            Transformation = new Transformation().Width(400).Height(200).Crop("fit"),
            Overwrite      = true
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");

        _logger.LogInformation("Signature uploaded to Cloudinary: {Url}", uploadResult.SecureUrl);

        // Save to settings
        var update = Builders<Settings>.Update
            .Set(s => s.SignatureCloudinaryUrl, uploadResult.SecureUrl.ToString())
            .Set(s => s.SignaturePublicId,       uploadResult.PublicId)
            .Set(s => s.UpdatedAt,               DateTime.UtcNow);

        var updated = await _db.Settings.FindOneAndUpdateAsync<Settings, Settings>(
            _ => true,
            update,
            new FindOneAndUpdateOptions<Settings, Settings> { ReturnDocument = ReturnDocument.After }
        );

        return ToDto(updated);
    }

    public async Task<SettingsResponse> DeleteSignatureAsync()
    {
        var settings = await GetOrCreateSettingsAsync();

        if (!string.IsNullOrEmpty(settings.SignaturePublicId))
        {
            await _cloudinary.DestroyAsync(new DeletionParams(settings.SignaturePublicId));
        }

        var update = Builders<Settings>.Update
            .Set(s => s.SignatureCloudinaryUrl, (string?)null)
            .Set(s => s.SignaturePublicId,       (string?)null)
            .Set(s => s.UpdatedAt,               DateTime.UtcNow);

        var updated = await _db.Settings.FindOneAndUpdateAsync<Settings, Settings>(
            _ => true,
            update,
            new FindOneAndUpdateOptions<Settings, Settings> { ReturnDocument = ReturnDocument.After }
        );

        return ToDto(updated);
    }

    public async Task<Settings> GetInternalAsync()
        => await GetOrCreateSettingsAsync();

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private async Task<Settings> GetOrCreateSettingsAsync()
    {
        var settings = await _db.Settings.Find(_ => true).FirstOrDefaultAsync();

        if (settings is null)
        {
            // Create default settings if none exist (should be seeded, but safety net)
            settings = new Settings { UpdatedAt = DateTime.UtcNow };
            await _db.Settings.InsertOneAsync(settings);
        }

        return settings;
    }

    private static SettingsResponse ToDto(Settings s) => new(
        Id:                    s.Id,
        CompanyName:           s.CompanyName,
        CompanyAddress:        s.CompanyAddress,
        CompanyPhone:          s.CompanyPhone,
        CompanyEmail:          s.CompanyEmail,
        Gstin:                 s.Gstin,
        BankName:              s.BankName,
        BankAccount:           s.BankAccount,
        BankIfsc:              s.BankIfsc,
        SignatureCloudinaryUrl: s.SignatureCloudinaryUrl,
        HasSignature:          !string.IsNullOrEmpty(s.SignatureCloudinaryUrl),
        InvoicePrefix:         s.InvoicePrefix,
        GstRate:               s.GstRate,
        UpdatedAt:             s.UpdatedAt
    );
}
