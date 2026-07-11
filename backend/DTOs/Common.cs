namespace BillingSystem.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// ApiResponse<T> — Standard wrapper for ALL API responses.
// Every controller returns this. Frontend always gets { success, data, message }.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Standardized API response envelope used by all controllers.
/// Ensures frontend always has a consistent shape to work with.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// Factory: successful response with data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    /// <summary>
    /// Factory: error response without data.
    /// </summary>
    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

// ─────────────────────────────────────────────────────────────────────────────
// PagedResult<T> — Used by list endpoints that support pagination.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Pagination wrapper returned by list endpoints.</summary>
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
}
