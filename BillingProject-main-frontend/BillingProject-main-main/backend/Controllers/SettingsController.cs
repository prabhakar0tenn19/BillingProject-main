using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Company settings management (name, address, GST, bank, signature).
///
/// GET  /api/v1/settings           → Get current settings
/// PUT  /api/v1/settings           → Update settings
/// POST /api/v1/settings/signature → Upload digital signature (multipart)
/// DELETE /api/v1/settings/signature → Remove signature
/// </summary>
[Tags("Settings")]
public class SettingsController : BaseApiController
{
    private readonly ISettingsService _service;

    public SettingsController(ISettingsService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SettingsResponse>), 200)]
    public async Task<IActionResult> Get()
    {
        try
        {
            var settings = await _service.GetAsync();
            return OkResponse(settings);
        }
        catch (Exception ex)
        {
            return ServerErrorResponse(ex.Message);
        }
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<SettingsResponse>), 200)]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequestResponse("Invalid request data");

        try
        {
            var updated = await _service.UpdateAsync(request);
            return OkResponse(updated, "Settings updated successfully");
        }
        catch (Exception ex)
        {
            return ServerErrorResponse(ex.Message);
        }
    }

    /// <summary>
    /// Upload digital signature image.
    /// Accepts: multipart/form-data with field name "file"
    /// Supported formats: PNG, JPG, WEBP (transparent PNG recommended)
    /// </summary>
    [HttpPost("signature")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<SettingsResponse>), 200)]
    public async Task<IActionResult> UploadSignature(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequestResponse("No file provided");

        var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequestResponse("Only PNG, JPG, and WEBP images are supported");

        if (file.Length > 5 * 1024 * 1024)  // 5MB limit
            return BadRequestResponse("File size must be under 5MB");

        try
        {
            var updated = await _service.UploadSignatureAsync(file);
            return OkResponse(updated, "Signature uploaded successfully");
        }
        catch (Exception ex)
        {
            return ServerErrorResponse($"Upload failed: {ex.Message}");
        }
    }

    [HttpDelete("signature")]
    [ProducesResponseType(typeof(ApiResponse<SettingsResponse>), 200)]
    public async Task<IActionResult> DeleteSignature()
    {
        try
        {
            var updated = await _service.DeleteSignatureAsync();
            return OkResponse(updated, "Signature removed successfully");
        }
        catch (Exception ex)
        {
            return ServerErrorResponse(ex.Message);
        }
    }
}
