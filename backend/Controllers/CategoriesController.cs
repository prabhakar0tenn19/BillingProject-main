using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Product category management.
///
/// GET    /api/v1/categories       → List all active categories
/// POST   /api/v1/categories       → Create category
/// GET    /api/v1/categories/{id}  → Get by ID
/// PUT    /api/v1/categories/{id}  → Update
/// DELETE /api/v1/categories/{id}  → Soft delete
/// </summary>
[Tags("Categories")]
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var categories = await _service.GetAllAsync();
            return OkResponse(categories);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var category = await _service.GetByIdAsync(id);
            if (category is null) return NotFoundResponse("Category not found");
            return OkResponse(category);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var created = await _service.CreateAsync(request);
            return CreatedResponse(created, "Category created");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var updated = await _service.UpdateAsync(id, request);
            if (updated is null) return NotFoundResponse("Category not found");
            return OkResponse(updated, "Category updated");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFoundResponse("Category not found");
            return OkResponse(new { id }, "Category deleted");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }
}
