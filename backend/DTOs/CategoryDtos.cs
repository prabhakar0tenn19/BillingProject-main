using System.ComponentModel.DataAnnotations;

namespace BillingSystem.DTOs;

// ─── Category Request DTOs ────────────────────────────────────────────────────

public record CreateCategoryRequest(
    [Required(ErrorMessage = "Category name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    string Name,

    string? Description,

    [Required(ErrorMessage = "HSN code is required")]
    [MaxLength(10, ErrorMessage = "HSN code cannot exceed 10 characters")]
    string HsnCode
);

public record UpdateCategoryRequest(
    [Required] string Name,
    string? Description,
    [Required] string HsnCode
);

// ─── Category Response DTOs ───────────────────────────────────────────────────

public record CategoryResponse(
    string Id,
    string Name,
    string? Description,
    string HsnCode,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
