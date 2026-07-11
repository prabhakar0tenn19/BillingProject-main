using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Customer (party) management.
///
/// GET    /api/v1/customers          → List all (filter: search)
/// GET    /api/v1/customers/list     → Lightweight list for dropdowns
/// POST   /api/v1/customers          → Create customer
/// GET    /api/v1/customers/{id}     → Get by ID
/// PUT    /api/v1/customers/{id}     → Update
/// DELETE /api/v1/customers/{id}     → Soft delete
/// </summary>
[Tags("Customers")]
public class CustomersController : BaseApiController
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        try
        {
            var customers = await _service.GetAllAsync(search);
            return OkResponse(customers);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    /// <summary>Lightweight list used in dropdowns (New Bill party selector)</summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetList()
    {
        try
        {
            var list = await _service.GetListAsync();
            return OkResponse(list);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer is null) return NotFoundResponse("Customer not found");
            return OkResponse(customer);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var created = await _service.CreateAsync(request);
            return CreatedResponse(created, "Customer created");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCustomerRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var updated = await _service.UpdateAsync(id, request);
            if (updated is null) return NotFoundResponse("Customer not found");
            return OkResponse(updated, "Customer updated");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFoundResponse("Customer not found");
            return OkResponse(new { id }, "Customer deleted");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }
}
