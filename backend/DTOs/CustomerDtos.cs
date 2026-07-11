using System.ComponentModel.DataAnnotations;

namespace BillingSystem.DTOs;

// ─── Customer Request DTOs ────────────────────────────────────────────────────

public record CreateCustomerRequest(
    [Required(ErrorMessage = "Party name is required")]
    [MaxLength(200, ErrorMessage = "Party name cannot exceed 200 characters")]
    string PartyName,

    string? ContactPerson,

    [Phone(ErrorMessage = "Invalid phone number")]
    string? Phone,

    [EmailAddress(ErrorMessage = "Invalid email address")]
    string? Email,

    string? BillingAddress,
    string? ShippingAddress,
    string? Gstin,
    string? PanNumber
);

public record UpdateCustomerRequest(
    [Required] string PartyName,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? BillingAddress,
    string? ShippingAddress,
    string? Gstin,
    string? PanNumber,
    bool IsActive
);

// ─── Customer Response DTOs ───────────────────────────────────────────────────

public record CustomerResponse(
    string Id,
    string PartyName,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? BillingAddress,
    string? ShippingAddress,
    string? Gstin,
    string? PanNumber,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Minimal customer info for dropdowns / billing party selection.</summary>
public record CustomerListItem(
    string Id,
    string PartyName,
    string? Phone,
    string? Gstin,
    bool IsActive
);
