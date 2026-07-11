using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Product catalog management.
/// basePrice is accepted in create/update but NEVER returned in any response.
///
/// GET    /api/v1/products                 → List products (filter: categoryId, search)
/// POST   /api/v1/products                 → Create product
/// GET    /api/v1/products/{id}            → Get by ID
/// PUT    /api/v1/products/{id}            → Full update
/// DELETE /api/v1/products/{id}            → Soft delete
/// PATCH  /api/v1/products/{id}/stock      → Update stock
/// </summary>
[Tags("Products")]
public class ProductsController : BaseApiController
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? categoryId, [FromQuery] string? search)
    {
        try
        {
            var products = await _service.GetAllAsync(categoryId, search);
            return OkResponse(products);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var product = await _service.GetByIdAsync(id);
            if (product is null) return NotFoundResponse("Product not found");
            return OkResponse(product);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var created = await _service.CreateAsync(request);
            return CreatedResponse(created, "Product created");
        }
        catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProductRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var updated = await _service.UpdateAsync(id, request);
            if (updated is null) return NotFoundResponse("Product not found");
            return OkResponse(updated, "Product updated");
        }
        catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFoundResponse("Product not found");
            return OkResponse(new { id }, "Product deleted");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(string id, [FromBody] UpdateStockRequest request)
    {
        try
        {
            var updated = await _service.UpdateStockAsync(id, request);
            if (updated is null) return NotFoundResponse("Product not found");
            return OkResponse(updated, "Stock updated");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    /// <summary>
    /// Upload product image.
    /// Accepts: multipart/form-data with field name "file"
    /// Supported formats: PNG, JPG, WEBP
    /// </summary>
    [HttpPost("{id}/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), 200)]
    public async Task<IActionResult> UploadImage(string id, IFormFile file)
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
            var updated = await _service.UploadImageAsync(id, file);
            if (updated is null) return NotFoundResponse("Product not found");
            return OkResponse(updated, "Product image uploaded successfully");
        }
        catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpDelete("{id}/image")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), 200)]
    public async Task<IActionResult> DeleteImage(string id)
    {
        try
        {
            var updated = await _service.DeleteImageAsync(id);
            if (updated is null) return NotFoundResponse("Product not found");
            return OkResponse(updated, "Product image removed successfully");
        }
        catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }
}
