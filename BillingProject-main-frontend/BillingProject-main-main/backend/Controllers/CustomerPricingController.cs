using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

/// <summary>
/// Per-party product pricing management.
/// Allows setting different prices for the same product per customer.
///
/// GET    /api/v1/customers/{customerId}/pricing               → All price overrides for party
/// POST   /api/v1/customers/{customerId}/pricing               → Set/upsert custom price
/// PUT    /api/v1/customers/{customerId}/pricing/{productId}   → Update price
/// DELETE /api/v1/customers/{customerId}/pricing/{productId}   → Remove override (revert to base)
/// GET    /api/v1/customers/{customerId}/pricing/bill-ready    → All products with effective price (for billing)
/// </summary>
[ApiController]
[Route("api/v1/customers/{customerId}/pricing")]
[Tags("Customer Pricing")]
public class CustomerPricingController : BaseApiController
{
    private readonly ICustomerPricingService _service;

    public CustomerPricingController(ICustomerPricingService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetPricing(string customerId)
    {
        try
        {
            var pricing = await _service.GetPricingForCustomerAsync(customerId);
            return OkResponse(pricing);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    /// <summary>
    /// Returns all products with their effective price for this party.
    /// This is the endpoint called by the New Bill wizard when a party is selected.
    /// </summary>
    [HttpGet("bill-ready")]
    public async Task<IActionResult> GetBillReady(string customerId)
    {
        try
        {
            var products = await _service.GetBillReadyProductsAsync(customerId);
            return OkResponse(products);
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> SetPricing(string customerId, [FromBody] SetPricingRequest request)
    {
        if (!ModelState.IsValid) return BadRequestResponse("Invalid data");
        try
        {
            var pricing = await _service.SetPricingAsync(customerId, request);
            return CreatedResponse(pricing, "Price set successfully");
        }
        catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpPut("{productId}")]
    public async Task<IActionResult> UpdatePricing(string customerId, string productId, [FromBody] UpdatePricingRequest request)
    {
        try
        {
            var pricing = await _service.UpdatePricingAsync(customerId, productId, request);
            if (pricing is null) return NotFoundResponse("Pricing override not found");
            return OkResponse(pricing, "Price updated");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> DeletePricing(string customerId, string productId)
    {
        try
        {
            var deleted = await _service.DeletePricingAsync(customerId, productId);
            if (!deleted) return NotFoundResponse("Pricing override not found");
            return OkResponse(new { customerId, productId }, "Price override removed");
        }
        catch (Exception ex) { return ServerErrorResponse(ex.Message); }
    }
}
