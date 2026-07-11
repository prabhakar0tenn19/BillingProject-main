namespace BillingSystem.DTOs;

// ─── Settings Request DTOs ────────────────────────────────────────────────────

/// <summary>Used to update company-wide settings from the Settings page.</summary>
public record UpdateSettingsRequest(
    string CompanyName,
    string CompanyAddress,
    string CompanyPhone,
    string CompanyEmail,
    string Gstin,
    string BankName,
    string BankAccount,
    string BankIfsc,
    string InvoicePrefix,
    decimal GstRate
);

// ─── Settings Response DTOs ───────────────────────────────────────────────────

/// <summary>
/// Company settings returned to the frontend.
/// Excludes nextInvoiceNumber (internal counter, not exposed).
/// </summary>
public record SettingsResponse(
    string? Id,
    string CompanyName,
    string CompanyAddress,
    string CompanyPhone,
    string CompanyEmail,
    string Gstin,
    string BankName,
    string BankAccount,
    string BankIfsc,
    string? SignatureCloudinaryUrl,
    bool HasSignature,
    string InvoicePrefix,
    decimal GstRate,
    DateTime UpdatedAt
);
