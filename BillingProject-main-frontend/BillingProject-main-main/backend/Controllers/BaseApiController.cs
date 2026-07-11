using BillingSystem.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Base controller providing standardized response helpers.
/// All API controllers inherit from this.
///
/// Every API response uses ApiResponse&lt;T&gt; envelope:
/// {
///   "success": true/false,
///   "data": { ... },
///   "message": "optional message"
/// }
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult OkResponse<T>(T data, string? message = null)
        => Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult CreatedResponse<T>(T data, string? message = null)
        => StatusCode(201, ApiResponse<T>.Ok(data, message));

    protected IActionResult NotFoundResponse(string message)
        => NotFound(ApiResponse<object>.Fail(message));

    protected IActionResult BadRequestResponse(string message)
        => BadRequest(ApiResponse<object>.Fail(message));

    protected IActionResult ServerErrorResponse(string message)
        => StatusCode(500, ApiResponse<object>.Fail(message));
}
